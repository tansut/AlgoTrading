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

namespace Kalitte.Trading
{
    public class SignalResultX
    {
        public OrderSide? finalResult = null;
        public Signal Signal { get; set; }
        public DateTime SignalTime { get; set; }

        //public SignalResultX(Signal signal): this(signal)
        //{
        //}

        public SignalResultX(Signal signal, DateTime signalTime)
        {
            this.Signal = signal;
            this.SignalTime = signalTime;
        }

        public override string ToString()
        {
            return $"time: {SignalTime}, finalResult: {finalResult}";
        }
    }


    public class SignalEventArgs : EventArgs
    {
        public SignalResultX Result { get; set; }

    }

    public delegate void SignalEventHandler(Signal signal, SignalEventArgs data);

    public abstract class Signal
    {
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }
        public OrderSide? LastSignalResult { get; protected set; }
        public volatile bool IsRunning = false;

        protected Task collectorTask = null;
        protected CancellationTokenSource collectorTaskTokenSource;


        public ManualResetEvent InOperationLock = new ManualResetEvent(true);

        public event SignalEventHandler OnSignal;

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.Name}]";
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
            Algo.Log($"{this.Name}[{Thread.CurrentThread.ManagedThreadId}]: {message}", level, t ?? Algo.AlgoTime);
        }

        protected virtual void onTick(Object source, ElapsedEventArgs e)
        {
            var hasLock = false;
            var restartTimer = false;
            SignalResultX result = null;
            try
            {
                Monitor.TryEnter(_locker, ref hasLock);
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
                    Monitor.Exit(_locker);
                    if (restartTimer) _timer.Start();
                }
            }
            
        }

        protected abstract SignalResultX CheckInternal(DateTime? t = null);

        public virtual SignalResultX Check(DateTime? t = null)
        {
            InOperationLock.Reset();
            try
            {
                var result = CheckInternal(t);
                result.SignalTime = t ?? DateTime.Now;
                raiseSignal(new SignalEventArgs() { Result = result });
                LastSignalResult = result.finalResult ?? LastSignalResult;
                return result;
            } catch (Exception ex)
            {
                Log($"Signal {this.Name} got exception. {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                return new SignalResultX(this, t ?? DateTime.Now) {  finalResult = null };
            } finally
            {
                InOperationLock.Set();
            }
        }

        protected virtual void Colllect()
        {
            
        }

        //public virtual void Reset()
        //{
        //    if (!checkLock.WaitOne(30000))
        //    {
        //        Log("Timeout in waiting operation", LogLevel.Error);
        //    }
        //}

        protected virtual void ResetInternal()
        {

        }

        public virtual void Reset()
        {
            if (!InOperationLock.WaitOne(5000))
            {
                Log("Timeout in starting signal", LogLevel.Error);
            }
            else ResetInternal();
        }

        public virtual void Init()
        {

        }

        public virtual void Start()
        {
            if (!InOperationLock.WaitOne(5000))
            {
                Log("Timeout in starting signal", LogLevel.Error);
            }
            this.IsRunning = true;
            if (Enabled && TimerEnabled)
            {
                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += this.onTick;
                _timer.Start();
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
            if (Enabled && TimerEnabled)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            this.IsRunning = false;
            if (!InOperationLock.WaitOne(30000))
            {
                Log("Timeout in stopping signal", LogLevel.Error);
            }
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
