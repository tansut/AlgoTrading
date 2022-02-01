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

        public SignalResultX(Signal signal)
        {
            this.Signal = signal;
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
        public string Name { get; set; }
        public Kalitte.Trading.Algos.AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }

        public event SignalEventHandler OnSignal;

        public Signal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner)
        {
            Name = name;
            Symbol = symbol;
            Algo = owner;
            Enabled = true;
            TimerEnabled = false;
            Simulation = false;

        }

        protected virtual void onTick(Object source, ElapsedEventArgs e)
        {
            var result = this.Check();
            if (result != null) raiseSignal(new SignalEventArgs() { Result = result });
        }

        public abstract SignalResultX Check(DateTime? t = null);

        public virtual void Start()
        {
            if (Enabled && TimerEnabled)
            {
                _timer = new System.Timers.Timer(500);
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
        }


        protected virtual void raiseSignal(SignalEventArgs data)
        {
            if (this.OnSignal != null) OnSignal(this, data);
        }
    }


}
