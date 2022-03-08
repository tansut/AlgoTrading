﻿// algo
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

        public override string ToString()
        {
            return $"{base.ToString()}[{L1},{ L2},{IndicatorValue}]"; ;
        }
    }

    public class DoubleCrossSignal : Signal
    {
        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }
        public virtual decimal SignalSensitivity { get; set; } = 1.0M;

        public CrossSignal Cross1 { get; set; }
        public CrossSignal Cross2 { get; set; }

        public decimal Delta { get; set; }
        public decimal RedLine { get; set; }

        public ITechnicalIndicator Indicator { get; set; }

        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public bool L2Set { get; set; }

        public decimal DeltaRatio { get; set; } = 0.2M;

        public Custom L1Indicator { get; set; }
        public Custom L2Indicator { get; set; }

        public decimal AvgChange { get; set; } = 0.45M;

        public DoubleCrossSignal(string name, string symbol, AlgoBase owner, decimal delta) : base(name, symbol, owner)
        {
            Delta = delta;
        }

        public override void Init()
        {
            ResetLimits();

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

            Cross1.SignalSensitivity = this.SignalSensitivity;
            Cross2.SignalSensitivity = this.SignalSensitivity;

            Cross1.Init();
            Cross2.Init();

            base.Init();
        }

        protected void ResetLimits()
        {
            L1 = RedLine;
            L2 = L1 + Delta * 10;
            L2Set = false;
        }

        protected override void ResetInternal()
        {
            ResetLimits();
            Cross1.Reset();
            Cross2.Reset();
            base.ResetInternal();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new DoubleCrossSignalResult(this, Algo.Now);
            var c1Res = Cross1.Check(t);
            var c2Res = Cross2.Check(t);
            var i1Val = ((CrossSignalResult)c1Res).i1Val;

            if (i1Val != 0)
            {
                if (Delta < 0)
                {
                    if (i1Val > L1)
                    {
                        ResetInternal();
                        return result;
                    }

                    if (c1Res.finalResult == BuySell.Sell)
                    {
                        if (c2Res.finalResult == BuySell.Sell || !L2Set)
                        {
                            var newL2 = i1Val + Delta * DeltaRatio;
                            if (newL2 > L2 && L2Set) result.finalResult = BuySell.Buy;
                            L2 = newL2;
                            L2Set = true;
                        }
                    }
                    if (i1Val < L1 && (c2Res.finalResult == BuySell.Buy || c1Res.finalResult == BuySell.Buy))
                    {
                        result.finalResult = BuySell.Buy;
                    }
                }
                else
                {
                    if (i1Val < L1)
                    {
                        ResetInternal();
                        return result;
                    }
                    if (c1Res.finalResult == BuySell.Buy)
                    {
                        if (c2Res.finalResult == BuySell.Buy || !L2Set)
                        {
                            var newL2 = i1Val + Delta * DeltaRatio;
                            if (newL2 < L2 && L2Set) result.finalResult = BuySell.Sell;
                            L2 = newL2;
                            L2Set = true;
                        }
                    }
                    if (i1Val > L1 && (c2Res.finalResult == BuySell.Sell || c1Res.finalResult == BuySell.Sell))
                    {
                        result.finalResult = BuySell.Sell;
                    }
                }
            }
            result.IndicatorValue = i1Val;
            result.L1 = L1;
            result.L2 = L2;
            if (result.finalResult.HasValue) ResetInternal();
            return result;
        }
    }
}