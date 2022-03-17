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
    public class ClosePositionsSignalConfig : SignalConfig
    {
        [AlgoParam(0)]
        public decimal KeepQuantity { get; set; }
    }

    public class ClosePositionsSignalResult : SignalResult
    {
        public decimal Quantity { get; set; }

        public ClosePositionsSignalResult(SignalBase signal, DateTime signalTime) : base(signal, signalTime)
        {
        }
    }

    public class ClosePositionsSignal : Signal<ClosePositionsSignalConfig>
    {


        public ClosePositionsSignal(string name, string symbol, AlgoBase owner, ClosePositionsSignalConfig config) : base(name, symbol, owner, config)
        {

        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? DateTime.Now;

            var result = new ClosePositionsSignalResult(this, time);
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);
            
            if (time.Hour == 22 && time.Minute == 57)
            {
                result.finalResult = BuySell.Sell;
                result.Quantity = Config.KeepQuantity;
            }
            return result;
        }
    }
}
