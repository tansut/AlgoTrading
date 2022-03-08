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
        public decimal? BestValue { get; set; }
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
        public decimal Alfa { get; set; } = 0.02M;
        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? BestValue { get; set; }
        private FinanceList<decimal> criticalBars;


        public GradientSignal(string name, string symbol, AlgoBase owner, decimal l1, decimal l2) : base(name, symbol, owner)
        {
            L1 = l1;
            L2 = l2;
        }

        public override void Init()
        {            
            criticalBars = new FinanceList<decimal>(8);
            base.Init();
        }

        protected override void ResetInternal()
        {
            TargetValue = null;
            BestValue = null;
            criticalBars.Clear();
            base.ResetInternal();
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
                result.IndicatorValue = iVal;
                AnalyseList.Collect(iVal);
                criticalBars.Push(iVal);
                if (AnalyseList.Ready && criticalBars.IsFull)
                {
                    var currentValue = AnalyseList.LastValue;
                    var nextTarget = currentValue > L1 && currentValue < L2 ? currentValue + currentValue * Alfa:  currentValue - currentValue * Alfa;
                    BestValue = BestValue.HasValue ?  BestValue.Value : currentValue;
                    
                    TargetValue = !TargetValue.HasValue ? currentValue : TargetValue;

                    if (currentValue > L1 && currentValue < L2)
                    {
                        if (currentValue >= TargetValue.Value) TargetValue = nextTarget;
                        else if (currentValue < BestValue) result.finalResult = BuySell.Sell;
                        BestValue = currentValue;
                        //criticalBars.Clear();
                    }
                    else if (currentValue < L1 && currentValue > L2)
                    {
                        if (currentValue <= TargetValue.Value) TargetValue = nextTarget;
                        else if (currentValue > BestValue) result.finalResult = BuySell.Buy;
                        BestValue = currentValue;
                        //criticalBars.Clear();
                    }
                    else ResetInternal();                          
                }
            }
            result.L1 = L1;
            result.L2 = L2;
            result.BestValue = TargetValue;            
            if (result.finalResult.HasValue) ResetInternal();
            return result;
        }
    }
}
