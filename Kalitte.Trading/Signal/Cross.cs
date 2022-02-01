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
        private decimal split = 1M;

        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }


        public override SignalResultX Check(DateTime? t = null)
        {
            OrderSide? result = null;

            if (Algo.CrossAboveX(i1, i2, t)) result = OrderSide.Buy;
            else if (Algo.CrossBelowX(i1, i2, t)) result = OrderSide.Sell;

            if (Simulation) return new SignalResultX(this) { finalResult = result };

            if (firstSignal != result)
            {
                firstSignal = result;
                if (result.HasValue)
                {
                    Algo.Log($"Set first signal to {result}");
                    result = null;
                }
            }
            else if (result.HasValue)
            {
                var dif = Math.Abs(i1.CurrentValue - i2.CurrentValue);
                if (dif >= split)
                {
                    Algo.Log($"{result} confirmed with diff {dif}");
                    firstSignal = null;
                }
                else
                {
                    Algo.Log($"{result} NOT confirmed with diff {dif}");
                    result = null;
                }
            }

            return new SignalResultX(this) { finalResult = result };
        }
    }

}
