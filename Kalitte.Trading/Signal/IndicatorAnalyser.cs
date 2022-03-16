using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    internal class IndicatorAnalyser : AnalyserBase
    {
        public ITechnicalIndicator i1k;

        public IndicatorAnalyser(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {

        }

        public decimal ? GetCurrentValue()
        {
            return AnalyseList.Ready ? AnalyseList.LastValue: default(decimal?);
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new SignalResult(this, t ?? Algo.Now);

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            if (CollectList.Ready && mp >= 0)
            {
                decimal mpAverage = CollectList.LastValue;

                var l1 = i1k.NextValue(mpAverage).Value.Value;

                AnalyseList.Collect(l1);
            }

            return result;
        }
    }
}
