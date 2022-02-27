// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Matriks.Lean.Algotrader.Models;
using Kalitte.Trading.Algos;
using Skender.Stock.Indicators;
using Kalitte.Trading.Indicators;

namespace Kalitte.Trading
{




    public class SignalResult
    {
        public BuySell? finalResult = null;
        public Signal Signal { get; set; }
        public DateTime SignalTime { get; set; }


        public SignalResult(Signal signal, DateTime signalTime)
        {
            this.Signal = signal;
            this.SignalTime = signalTime;
        }

        public override string ToString()
        {
            return $"time: {SignalTime}, finalResult: {finalResult}";
        }

        public override int GetHashCode()
        {
            return finalResult.GetHashCode();
        }
    }


    public class SignalEventArgs : EventArgs
    {
        public SignalResult Result { get; set; }
    }

    public delegate void SignalEventHandler(Signal signal, SignalEventArgs data);

    public abstract class Signal
    {
        public StartableState State { get; set; } = StartableState.Stopped;
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }
        public SignalResult LastSignalResult { get; protected set; }
        public DateTime? PausedUntil { get; set; }
        protected Task collectorTask = null;
        protected CancellationTokenSource collectorTaskTokenSource;

        public PerformanceMonitor PerfMon { get; set; }

        public List<ITechnicalIndicator> Indicators { get; set; } = new List<ITechnicalIndicator>();

        public ManualResetEvent InOperationLock = new ManualResetEvent(true);

        public event SignalEventHandler OnSignal;

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.Name}]";
        }

        public void Pause(DateTime until)
        {            
            PausedUntil = until;
            if (this.State == StartableState.Started)
            {
                //this.Stop();
                this.State = StartableState.Paused;
            }
        }



        public void MonitorInit(string name, decimal value)
        {
            if (PerfMon != null)
            {
                PerfMon.Init($"{this.Name}/{name}", value);
            }
        }

        public void Monitor(string name, decimal value)
        {
            if (PerfMon != null)
            {
                PerfMon.Set($"{this.Name}/{name}", value);
            }
        }

        private void CheckPause(DateTime? t)
        {
            if (this.State != StartableState.Paused) return;
            var time = t ?? DateTime.Now;
            if (time >= PausedUntil)
            {
                //this.Start();
                this.State = StartableState.Started;
            }
        }

        public Signal(string name, string symbol, AlgoBase owner)
        {
            Name = name;
            Symbol = symbol;
            Algo = owner;
            Enabled = true;
            TimerEnabled = false;
            Simulation = false;
            LastSignalResult = null;
            //CheckCount = 0;
        }


        public void Log(string message, LogLevel level, DateTime? t = null)
        {
            var th = Algo.Simulation ? "x" : Thread.CurrentThread.ManagedThreadId.ToString();
            Algo.Log($"{this.Name}[{th}]: {message}", level, t ?? Algo.AlgoTime);
        }

        protected virtual void onTick(Object source, ElapsedEventArgs e)
        {
            var hasLock = false;
            var restartTimer = false;
            SignalResult result = null;
            try
            {
                System.Threading.Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    return;
                }
                if (_timer != null && _timer.Enabled)
                {
                    restartTimer = true;
                }
                result = this.Check();
            }
            finally
            {
                if (hasLock)
                {
                    System.Threading.Monitor.Exit(_locker);
                    if (restartTimer) _timer.Start();
                }
            }
        }

        protected abstract SignalResult CheckInternal(DateTime? t = null);

        public virtual SignalResult Check(DateTime? t = null)
        {
            if (!InOperationLock.WaitOne()) return null;            
            InOperationLock.Reset();
            try
            {
                if (this.State == StartableState.Paused) return null;
                if (!EnsureUsingRightBars(t ?? DateTime.Now))
                {
                    Log($"IMPORTANT: Detected wrong bars for indicators.Time is: {t}", LogLevel.Error, t);
                    return null;
                }

                var result = CheckInternal(t);
                if (result != null)
                {
                    result.SignalTime = t ?? DateTime.Now;
                    raiseSignal(new SignalEventArgs() { Result = result });
                    LastSignalResult = result;
                }
                return result;
            }
            catch (Exception ex)
            {
                Log($"Signal {this.Name} got exception. {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                return new SignalResult(this, t ?? DateTime.Now) { finalResult = null };
            }
            finally
            {
                InOperationLock.Set();
            }
        }

        protected virtual void Colllect()
        {

        }


        protected virtual void ResetInternal()
        {

        }

        public virtual void Reset()
        {
            InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                ResetInternal();
            }
            finally
            {
                InOperationLock.Set();
            }
        }


        public virtual void Init()
        {
            
        }

        protected virtual bool EnsureUsingRightBars(DateTime t)
        {
            var result = true;
            foreach (var indicator in Indicators)
            {
                var expected = Algo.GetExpectedBarPeriod(t, indicator.InputBars.Period);
                if (expected != indicator.InputBars.Last.Date) return false;
            }
            return result;
        }

        protected virtual void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {

        }

        protected virtual void InputbarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                LoadNewBars(sender, e);
                Log($"Loaded new bars for {this.Name} Data: {e.Action}, {e.Item}", LogLevel.Verbose);
            }
            finally
            {
                InOperationLock.Set();
            }
        }

        public virtual void Start()
        {
            this.State = StartableState.StartInProgress;
            InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                if (Enabled && TimerEnabled)
                {
                    _timer = new System.Timers.Timer(1000);
                    _timer.Elapsed += this.onTick;
                    _timer.Start();
                }
                this.State = StartableState.Started;
            }
            finally
            {
                InOperationLock.Set();
            }



            //collectorTaskTokenSource = new CancellationTokenSource();
            //collectorTask = new Task(() =>
            //{
            //    collectorTaskTokenSource.Token.ThrowIfCancellationRequested();                
            //    while (!collectorTaskTokenSource.Token.IsCancellationRequested)
            //    {                    
            //        //Log($"{this.Name }task doing {Simulation}");
            //        //if (!Simulation) Colllect();
            //        Thread.Sleep(1000);
            //    }
            //});            
            //collectorTask.Start();

        }

        public virtual void Stop()
        {
            this.State = StartableState.StopInProgress;
            InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                if (Enabled && TimerEnabled)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    _timer = null;
                }
                this.State = StartableState.Stopped;
            }
            finally
            {
                InOperationLock.Set();
            }


            //if (!InOperationLock.WaitOne(30000))
            //{
            //    Log("Timeout in stopping signal", LogLevel.Error);
            //}
            //try
            //{
            //    collectorTaskTokenSource.Cancel();
            //    try
            //    {
            //        collectorTask.Wait(collectorTaskTokenSource.Token);
            //    }
            //    catch (OperationCanceledException)
            //    {

            //    }
            //}
            //catch (Exception ex)
            //{
            //    Log($"Error stopping task {this.Name}. {ex.Message}", LogLevel.Error);
            //}

            //Log($"Stopped.", LogLevel.Debug);


        }




        protected virtual void raiseSignal(SignalEventArgs data)
        {
            if (this.OnSignal != null) OnSignal(this, data);
            //if (data.Result.finalResult.HasValue) LastSignal = data.Result;
        }
    }


}
