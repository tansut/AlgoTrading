﻿// algo
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class GradientSignalResult : SignalResult
    {

        public GradientResult Gradient { get; set; }
        public decimal IndicatorValue { get; set; }


        public GradientSignalResult(Signal signal, DateTime signalTime) : base(signal, signalTime)
        {
        }
        public override string ToString()
        {
            var iv = IndicatorValue.ToString(".##");
            return $"{base.ToString()}[iVal: {iv}, {Gradient}]"; ;
        }

    }

    public class GradientSignal : AnalyserBase
    {
        public ITechnicalIndicator Indicator { get; set; }
        public decimal Tolerance { get; set; } = 0.010M;
        public decimal LearnRate { get; set; } = 0.005M;

        public decimal L1 { get; set; }
        public decimal L2 { get; set; }

        public Gradient grad { get; set; }


        public GradientSignal(string name, string symbol, AlgoBase owner, decimal l1, decimal l2) : base(name, symbol, owner)
        {
            L1 = l1;
            L2 = l2;
        }

        public override void Init()
        {
            grad = new Gradient(L1, L2, this.Algo);
            grad.FileName = Algo.MultipleTestOptimization ? "": Path.Combine(Algo.LogDir, this.Name + ".png");
            grad.Tolerance = Tolerance;
            grad.LearnRate = LearnRate;
            base.Init();
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
                
                if (AnalyseList.Ready)
                { 
                    result.Gradient = grad.Step(AnalyseList.LastValue);
                }
            }

            if (result.Gradient != null)
            {
                result.finalResult = result.Gradient.FinalResult;
                if (result.finalResult.HasValue) ResetInternal();
                else if (result.Gradient.OutOfRange) ResetInternal();
            }

            return result;
        }
    }
}
