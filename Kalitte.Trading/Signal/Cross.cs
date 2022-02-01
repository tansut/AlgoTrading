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
    public class CrossSignal : Signal
    {
        public IIndicator i1 = null;
        public IIndicator i2 = null;

        private OrderSide? firstSignal = null;

        public decimal Split = 0.3M;

        public int Periods = 5;
        private List<decimal> lastSignals = new List<decimal>();
        private OrderSide? initialPeriodSignal = null;

        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        protected  SignalResultX CalculateSignal(DateTime? t = null)
        {
            OrderSide? currentSignal = null;
            OrderSide? periodSignal = null;

            if (Algo.CrossAboveX(i1, i2, t)) periodSignal = OrderSide.Buy;
            else if (Algo.CrossBelowX(i1, i2, t)) periodSignal = OrderSide.Sell;

            //periodSignal = OrderSide.Sell;
            //Algo.Log($"cacl initial period {initialPeriodSignal}", LogLevel.Debug);

            if (!initialPeriodSignal.HasValue && periodSignal.HasValue)
            {
                initialPeriodSignal = periodSignal;
                Algo.Log($"Setting initial period signal to {initialPeriodSignal}", LogLevel.Debug);
            }

            if (CheckCount == 0 && !initialPeriodSignal.HasValue)
            {
                Algo.Log($"No initial signal for {Name}. Suspended until a valid initial signal", LogLevel.Warning);
            }

            if (!initialPeriodSignal.HasValue) return new SignalResultX(this) { finalResult = null };

            if (i1.CurrentValue > i2.CurrentValue) currentSignal = OrderSide.Buy;
            else if (i1.CurrentValue < i2.CurrentValue) currentSignal = OrderSide.Sell;

            if (Simulation) return new SignalResultX(this) { finalResult = periodSignal };

            var dif = i1.CurrentValue - i2.CurrentValue;
            //Algo.Log($"{i1.CurrentValue} - {i2.CurrentValue}", LogLevel.Debug);

            lastSignals.Add(dif);
            if (lastSignals.Count > Periods) lastSignals.RemoveAt(0);

            OrderSide? finalResult = null;

            if (lastSignals.Count >= Periods)
            {                
                var avg = lastSignals.Average();
                Algo.Log($"Average: {avg} slip: {Split}");
                if (avg >= Split) finalResult = OrderSide.Buy;
                else if (avg <= -Split) finalResult = OrderSide.Sell;
            }

            return new SignalResultX(this) { finalResult = finalResult };
        }

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var current = CalculateSignal(t);
            //if (current.finalResult == LastSignal) return new SignalResultX(this) { finalResult = null };
            return current;
         }
    }

}
