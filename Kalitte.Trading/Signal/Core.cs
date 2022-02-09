// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class SignalResultX
    {
        public OrderSide? finalResult = null;
        public Signal Signal { get; set; }
        public DateTime SignalTime { get; set; }

        public SignalResultX(Signal signal): this(signal, DateTime.Now)
        {
        }

        public SignalResultX(Signal signal, DateTime signalTime)
        {
            this.Signal = signal;
            this.SignalTime = signalTime;
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
        public Kalitte.Trading.Algos.AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }
        public OrderSide? LastSignalResult { get; protected set; }
        protected volatile bool isRunning = false;

        protected Task collectorTask = null;
        protected CancellationTokenSource collectorTaskTokenSource;


        private ManualResetEvent checkLock = new ManualResetEvent(true);

        public event SignalEventHandler OnSignal;

        public Signal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner)
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
            Algo.Log($"{this.Name}[{Thread.CurrentThread.ManagedThreadId}]: {message}", level, t);
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
            try
            {
                var result = CheckInternal(t);
                result.SignalTime = t ?? DateTime.Now;
                raiseSignal(new SignalEventArgs() { Result = result });
                LastSignalResult = result.finalResult;
                return result;
            } catch (Exception ex)
            {
                Log($"Signal {this.Name} got exception. {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                return new SignalResultX(this) {  finalResult = null };
            }
        }

        protected virtual void Colllect()
        {
            
        }
        
        public virtual void Start()
        {
            this.isRunning = true;

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
            if (Enabled && TimerEnabled)
            {
                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += this.onTick;
                _timer.Start();
            }
        }

        public virtual void Stop()
        {
            if (Enabled && TimerEnabled)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            this.isRunning = false;
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

            Log($"Stopped.", LogLevel.Debug);


        }

        


        protected virtual void raiseSignal(SignalEventArgs data)
        {
            if (this.OnSignal != null) OnSignal(this, data);
            //if (data.Result.finalResult.HasValue) LastSignal = data.Result;
        }
    }


}
