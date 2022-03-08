// algo
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class GradientSignalResult : SignalResult
    {
        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public decimal IndicatorValue { get; set; }


        public GradientSignalResult(Signal signal, DateTime signalTime) : base(signal, signalTime)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}[{L1},{ L2},{IndicatorValue}]"; ;
        }
    }

    public class GradientSignal : AnalyserBase
    {
        public ITechnicalIndicator Indicator { get; set; }
        public decimal Alfa { get; set; } = 0.2M;
        public decimal L1 { get; set; }
        public decimal L2 { get; set; }


        public GradientSignal(string name, string symbol, AlgoBase owner, decimal l1, decimal l2) : base(name, symbol, owner)
        {
            L1 = l1;
            L2 = l2;
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new GradientSignalResult(this, Algo.Now);
            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            if (CollectList.Ready && mp > 0)
            {
                decimal mpAverage = CollectList.LastValue;

                var iVal = Indicator.NextValue(mpAverage).Value.Value;

                AnalyseList.Collect(iVal);

                if (AnalyseList.Ready)
                {
                    var lastAvg = AnalyseList.LastValue;

                    //result.i1Val = l1;
                    //result.i2Val = l2;
                    //result.Dif = lastAvg;

                    //if (lastAvg > AvgChange) result.finalResult = BuySell.Buy;
                    //else if (lastAvg < -AvgChange) result.finalResult = BuySell.Sell;
                }
            }
            return result;
        }
    }
}
