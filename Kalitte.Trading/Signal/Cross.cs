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
        private decimal split = 0.3M;
        private int periods = 5;
        private List<decimal> lastSignals = new List<decimal>();
        private OrderSide? initialPeriodSignal;

        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }


        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            OrderSide? currentSignal = null;
            OrderSide? periodSignal = null;

            if (Algo.CrossAboveX(i1, i2, t)) periodSignal = OrderSide.Buy;
            else if (Algo.CrossBelowX(i1, i2, t)) periodSignal = OrderSide.Sell;

            if (!initialPeriodSignal.HasValue && periodSignal.HasValue)
            {
                initialPeriodSignal = periodSignal;
            }

            if (!initialPeriodSignal.HasValue) return new SignalResultX(this) { finalResult = null };

            if (i1.CurrentValue > i2.CurrentValue) currentSignal = OrderSide.Buy;
            else if (i1.CurrentValue < i2.CurrentValue) currentSignal = OrderSide.Sell;

            if (Simulation) return new SignalResultX(this) { finalResult = periodSignal };

            if (firstSignal != currentSignal)
            {
                lastSignals.Clear();
                if (currentSignal.HasValue)
                {
                    Algo.Log($"Set first signal to {currentSignal}");
                    firstSignal = currentSignal;
                    currentSignal = null;
                }
                else if (firstSignal.HasValue)
                {
                    Algo.Log($" first signal: {firstSignal} going to null");
                    firstSignal = currentSignal;
                }
            }
            else if (currentSignal.HasValue)
            {
                var dif = Math.Abs(i1.CurrentValue - i2.CurrentValue);
                lastSignals.Add(dif);
                if (lastSignals.Count >= periods)
                {
                    var avg = lastSignals.Average();
                    lastSignals.RemoveAt(0);
                    if (avg >= split)
                    {
                        Algo.Log($"{currentSignal} confirmed with diff {dif}");
                        lastSignals.Clear();
                        firstSignal = null;
                    }
                    else
                    {
                        Algo.Log($"{currentSignal} NOT confirmed with diff {dif}");
                        currentSignal = null;
                    }
                }
                else
                {
                    currentSignal = null;
                }


            }



            return new SignalResultX(this) { finalResult = currentSignal };
        }
    }

}
