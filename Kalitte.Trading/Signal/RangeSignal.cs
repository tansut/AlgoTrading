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
    public class RangeSignal : Signal
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public IIndicator Indicator { get; set; }

        public RangeSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator indicator,
            decimal? min, decimal? max) : base(name, symbol, owner)
        {
            Min = min;
            Max = Max;
        }


        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            OrderSide? result = null;

            //if (!Max.HasValue && Min.HasValue) result = Indicator.CurrentValue > Min.Value ? OrderSide;



            return new SignalResultX(this) { finalResult = result };
        }

    }


}
