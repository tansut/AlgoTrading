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
    public class ClosePositionsSignal : SignalBase
    {
        public bool AutoClose { get; set; }        

        public ClosePositionsSignal(string name, string symbol, AlgoBase owner, bool autoClose) : base(name, symbol, owner)
        {
            this.AutoClose = AutoClose;
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? DateTime.Now;
            BuySell? bs = null;
            if (time.Hour == 22 && time.Minute == 59) bs = BuySell.Sell;
            return new SignalResult(this, t ?? DateTime.Now) { finalResult = bs };
        }
    }


}
