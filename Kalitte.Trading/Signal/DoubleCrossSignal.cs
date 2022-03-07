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

    public class DoubleCrossSignalResult : SignalResult
    {
        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public decimal IndicatorValue { get; set; }

        public DoubleCrossSignalResult(Signal signal, DateTime signalTime) : base(signal, signalTime)
        {
        }
    }

    public class DoubleCrossSignal : Signal
    {
        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }

        public CrossSignal Cross1 { get; set; }
        public CrossSignal Cross2 { get; set; }

        public decimal Delta { get; set; }
        public decimal RedLine { get; set; }

        public ITechnicalIndicator Indicator { get; set; }

        public decimal L1 { get; set; }
        public decimal L2 { get; set; }

        public Custom L1Indicator { get; set; }
        public Custom L2Indicator { get; set; }

        public decimal AvgChange { get; set; } = 0.5M;

        public DoubleCrossSignal(string name, string symbol, AlgoBase owner, decimal delta) : base(name, symbol, owner)
        {
            Delta = delta;
        }

        public override void Init()
        {
            L1 = RedLine;
            L2 = RedLine + Delta;

            this.Cross1 = new CrossSignal($"{this.Name}-c1", Symbol, Algo);
            this.Cross2 = new CrossSignal($"{this.Name}-c2", Symbol, Algo);
            this.L1Indicator = new Custom((v) => L1, this.Indicator.InputBars);
            this.L2Indicator = new Custom((v) => L2, this.Indicator.InputBars);

            Cross1.i1k = Indicator;
            Cross1.i2k = L1Indicator;
            Cross2.i1k = Indicator;
            Cross2.i2k = L2Indicator;

            Cross1.AvgChange = AvgChange;
            Cross2.AvgChange = AvgChange;

            Cross1.DynamicCross = false;
            Cross2.DynamicCross = false;

            Cross1.CollectSize = CollectSize;
            Cross2.CollectSize = CollectSize;

            Cross1.AnalyseSize = AnalyseSize;
            Cross2.AnalyseSize = AnalyseSize;

            Cross1.Init();
            Cross2.Init();

            base.Init();
        }

        protected override void ResetInternal()
        {
            L1 = RedLine;
            L2 = RedLine;
            Cross1.Reset();
            Cross2.Reset();
            base.ResetInternal();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new DoubleCrossSignalResult(this, Algo.Now);
            var c1Res = Cross1.Check(t);
            var c2Res = Cross2.Check(t);
            var i1Val = ((CrossSignalResult)c2Res).i1Val;

            if (i1Val != 0)
            {
                if (Delta < 0)
                {
                    if (i1Val > L1)
                    {
                        L1 = RedLine;
                        L2 = RedLine;
                    }

                    if (c1Res.finalResult == BuySell.Sell)
                    {
                        if (c2Res.finalResult == BuySell.Sell)
                        {
                            L2 = i1Val + Delta;
                        }
                    }
                    if (c2Res.finalResult == BuySell.Buy)
                    {
                        if (i1Val <= L2)
                            result.finalResult = BuySell.Buy;
                    }
                    else if (c1Res.finalResult == BuySell.Buy)
                    {
                        if (i1Val <= L2) result.finalResult = BuySell.Buy;
                    }
                }
                else
                {
                    if (i1Val < L1)
                    {
                        L1 = RedLine;
                        L2 = RedLine;
                    }
                    if (c1Res.finalResult == BuySell.Buy)
                    {
                        if (c2Res.finalResult == BuySell.Buy)
                        {
                            L2 = i1Val + Delta;
                        }
                    }
                    if (c2Res.finalResult == BuySell.Sell)
                    {
                        if (i1Val >= L2)
                            result.finalResult = BuySell.Sell;
                    }
                    else if (c1Res.finalResult == BuySell.Sell)
                    {
                        if (i1Val >= L2) result.finalResult = BuySell.Sell;
                    }
                }
            }
            result.IndicatorValue = i1Val;
            result.L1 = L1;
            result.L2 = L2;
            return result;
        }
    }
}
