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
using Kalitte.Trading.DataVisualization;

namespace Kalitte.Trading
{


    public class SignalConfig: ConfigParameters
    {
        [AlgoParam(true)]
        public bool Enabled { get; set; }

        [AlgoParam(OrderUsage.Unknown)]
        public OrderUsage Usage { get; internal set; }
    }



    public class SignalResult
    {
        public BuySell? finalResult = null;
        public SignalBase Signal { get; set; }
        public DateTime SignalTime { get; set; }


        public SignalResult(SignalBase signal, DateTime signalTime)
        {
            this.Signal = signal;
            this.SignalTime = signalTime;
        }

        public override string ToString()
        {
            return $"{SignalTime}/{finalResult}";
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

    public delegate void SignalEventHandler(SignalBase signal, SignalEventArgs data);


    public abstract class Signal<C> : SignalBase where C : SignalConfig 
    {
        public C Config { get; set; }

        protected Signal(string name, string symbol, AlgoBase owner, C config) : base(name, symbol, owner)
        {
            this.Config = config;
            this.Enabled = config.Enabled;
        }

        public override OrderUsage Usage
        {
            get
            {
                return Config.Usage;
            }
            protected set
            {
                // no action, can be set only by config?
            }
        }
    }

    public abstract class SignalBase
    {
        public StartableState State { get; set; } = StartableState.Stopped;
        protected System.Timers.Timer _timer = null;
        public ChartList Charts  { get; set; } = new ChartList();
        protected object OperationLock = new object();

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
        public int CompletedOrder { get; set; } = 0;
        public decimal CompletedQuantity { get; set; } = 0;

        public DateTime FirstOrderDate { get; set; } = DateTime.MinValue;
        public DateTime LastOrderDate { get; set; } = DateTime.MinValue;

        public virtual OrderUsage Usage { get; protected set; } = OrderUsage.Unknown;

        public PerformanceMonitor PerfMon { get; set; }

        public List<ITechnicalIndicator> Indicators { get; set; } = new List<ITechnicalIndicator>();

        public event SignalEventHandler OnSignal;

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.Name}]";
        }

        public Chart Chart(string name, string title = "")
        {
            return Charts.Chart($"{this.Name}-{name}", title);
        }

        public void SaveChart(string name, string id = "", bool clear = true)
        {
            Chart(name).Save(Path.Combine(Algo.LogDir, $"{this.Name}-{name}{id}.png"), clear);            
        }

        public void SaveChart(string name, DateTime time, bool clear = true)
        {
            var id = time.ToString("dd-MM-yy-HH-mm-ss");
            SaveChart(name, id, clear);
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

        public virtual void ResetOrdersInternal()
        {
            CompletedOrder = 0;
            CompletedQuantity = 0;
        }

        public void ResetOrders()
        {
            Monitor.Enter(OperationLock);
            try
            {
                ResetOrdersInternal();
            }
            finally
            {
                Monitor.Exit(OperationLock);
            }
        }

        public virtual void AddOrder(int orderInc, decimal quantityInc)
        {
            var time = Algo.Now;
            if (FirstOrderDate == DateTime.MinValue) FirstOrderDate = time;
            LastOrderDate = time;
            CompletedOrder += orderInc;
            CompletedQuantity += quantityInc;
        }

        public void MonitorInit(string name, decimal value)
        {
            if (PerfMon != null)
            {
                PerfMon.Init($"{this.Name}/{name}", value);
            }
        }

        public void Watch(string name, decimal value)
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

        public SignalBase(string name, string symbol, AlgoBase owner)
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
            Algo.Log($"{this.Name}[{th}]: {message}", level, t ?? Algo.Now);
        }

        protected virtual void onTick(Object source, ElapsedEventArgs e)
        {
            this.Check();
        }

        protected abstract SignalResult CheckInternal(DateTime? t = null);

        public virtual SignalResult Check(DateTime? t = null)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(OperationLock, ref lockTaken);
                if (!lockTaken)
                {
                    return null;
                }

                var time = t ?? Algo.Now;
                try
                {
                    if (this.State == StartableState.Paused) return null;
                    if (!Enabled) return null;
                    if (!EnsureUsingRightBars(time))
                    {
                        //Log($"IMPORTANT: Detected wrong bars for indicators.", LogLevel.Error, t);
                        return null;
                    }

                    var result = CheckInternal(t);
                    if (result != null)
                    {
                        result.SignalTime = Algo.Now;
                        raiseSignal(new SignalEventArgs() { Result = result });
                        LastSignalResult = result;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Log($"Signal {this.Name} got exception. {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                    return null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(OperationLock);
                }
            }
        }

        protected virtual void Colllect()
        {

        }


        protected virtual void ResetInternal()
        {
            ResetOrdersInternal();
        }

        public virtual void Reset()
        {
            Monitor.Enter(OperationLock);
            try
            {
                ResetInternal();
            }
            finally
            {
                Monitor.Exit(OperationLock);
            }
        }


        public virtual void Init()
        {

        }

        public bool BarDifferenceNegligible(DateTime t, DateTime expected, DateTime existing, BarPeriod period)
        {
            Helper.SymbolSeconds(period.ToString(), out int seconds);
            if (period == BarPeriod.Min10 && t.Hour == 18 && t.Minute == 15)
                seconds = seconds / 2;
            var rup = Helper.RoundUp(t, TimeSpan.FromSeconds(seconds));
            var rdown = Helper.RoundDown(t, TimeSpan.FromSeconds(seconds));
            var difference = Math.Min(Math.Abs((rup - t).TotalSeconds), Math.Abs((rup - t).TotalSeconds));
            var tolerance = 10;

            if (rup == rdown) return true;
            if (difference < tolerance) return true;
            return false;

        }

        protected virtual bool EnsureUsingRightBars(DateTime t)
        {
            var result = true;
            foreach (var indicator in Indicators)
            {
                var expected = Algo.GetExpectedBarPeriod(t, indicator.InputBars.Period);
                if (expected != indicator.InputBars.Last.Date)
                {
                    if (!BarDifferenceNegligible(t, expected, indicator.InputBars.Last.Date, indicator.InputBars.Period))
                    {
                        Log($"Wrong bar[{Thread.CurrentThread.ManagedThreadId}]: now: {t} expected:{expected} using:{indicator.InputBars.Last.Date}", LogLevel.Error, t);
                        return false;
                    } else
                    {
                        Log($"Wrong bar neglegted: now: {t} expected:{expected} using:{indicator.InputBars.Last.Date}", LogLevel.Warning, t);
                    }
                }
            }
            return result;
        }

        protected virtual void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {

        }



        protected virtual void InputbarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            Log($"Loading new bars for {this.Name} Data: {e.Action}, {e.Item}", LogLevel.Verbose);
            //Monitor.Enter(OperationLock);
            try
            {
                LoadNewBars(sender, e);
                Log($"Loaded new bars for {this.Name} Data: {e.Action}, {e.Item}", LogLevel.Debug);
            }
            finally
            {
                //Monitor.Exit(OperationLock);
            }
        }

        public virtual void Start()
        {
            this.State = StartableState.StartInProgress;
            Monitor.Enter(OperationLock);
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
                Monitor.Exit(OperationLock);
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
            Monitor.Enter(OperationLock);
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
                Monitor.Exit(OperationLock);
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
