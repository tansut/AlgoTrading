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
    public class FlipFlopSignal : Signal
    {
        public OrderSide Side { get; set; }

        public FlipFlopSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, OrderSide side = OrderSide.Buy) : base(name, symbol, owner)
        {
            this.Side = side;
        }


        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var result = this.Side;
            this.Side = result == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            return new SignalResultX(this) { finalResult = result };
        }




    }


}
