// algo
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


        public GradientSignalResult(SignalBase signal, DateTime signalTime) : base(signal, signalTime)
        {
        }
        public override string ToString()
        {
            var iv = IndicatorValue.ToString(".##");
            return $"{base.ToString()}[iVal: {iv}, {Gradient}]"; ;
        }

    }

    public class GradientSignalConfig : AnalyserConfig
    {
        [AlgoParam(0.01)]
        public decimal Tolerance { get; set; }

        [AlgoParam(0.005)]
        public decimal LearnRate { get; set; }

        [AlgoParam(0)]
        public decimal L1 { get; set; }

        [AlgoParam(0)]
        public decimal L2 { get; set; }
    }

    public class GradientSignal : AnalyserBase<GradientSignalConfig>
    {
        public ITechnicalIndicator Indicator { get; set; }
        public Gradient grad { get; set; }
        public BuySell DefaultAction { get; set; }

        public override OrderUsage Usage { get => base.Usage == OrderUsage.Unknown ? OrderUsage.CreatePosition : base.Usage; protected set => base.Usage = value; }

        public GradientSignal(string name, string symbol, AlgoBase owner, GradientSignalConfig config, BuySell defaultAction) : base(name, symbol, owner, config)
        {
            this.DefaultAction = defaultAction;
        }

        public override void Init()
        {
            grad = new Gradient(Config.L1, Config.L2, this.Algo);            
            var dir = Path.Combine(Algo.Appdir, "charts", this.Name);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            grad.FileName = Algo.MultipleTestOptimization ? "" : Path.Combine(dir, "grad.png");
            grad.Tolerance = Config.Tolerance;
            grad.LearnRate = Config.LearnRate;
            base.Init();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? Algo.Now;

            var result = new GradientSignalResult(this, time);

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0)
            {
                CollectList.Collect(mp, time);
                decimal mpAverage = CollectList.LastValue();
                var iVal = Indicator.NextValue(mpAverage).Value.Value;
                result.IndicatorValue = iVal;
                AnalyseList.Collect(iVal, time);
                var checkValue = AnalyseList.LastValue(Lookback);
                result.Gradient = grad.Step(checkValue);
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
