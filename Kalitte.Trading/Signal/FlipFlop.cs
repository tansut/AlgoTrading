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
using Kalitte.Trading.Algos;

namespace Kalitte.Trading
{
    public class FlipFlopSignal : Signal
    {
        public BuySell Side { get; set; }

        public FlipFlopSignal(string name, string symbol, AlgoBase owner, BuySell side = BuySell.Buy) : base(name, symbol, owner)
        {
            this.Side = side;
        }


        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var result = this.Side;
            this.Side = result == BuySell.Buy ? BuySell.Sell : BuySell.Buy;
            return new SignalResultX(this, t ?? DateTime.Now) { finalResult = result };
        }


    }


}
