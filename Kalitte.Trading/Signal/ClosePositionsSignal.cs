﻿// algo
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
    public class ClosePositionsSignalConfig : SignalConfig
    {

    }

    public class ClosePositionsSignal : Signal<ClosePositionsSignalConfig>
    {
        public ClosePositionsSignal(string name, string symbol, AlgoBase owner, ClosePositionsSignalConfig config) : base(name, symbol, owner, config)
        {
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
