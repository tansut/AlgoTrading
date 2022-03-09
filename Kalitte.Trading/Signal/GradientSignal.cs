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
        public decimal? FirstValue { get; set; }
        public decimal? LastValue { get; set; }
        public decimal? ResistanceValue { get; set; }
        public decimal? UsedValue { get; set; }
        public decimal IndicatorValue { get; set; }


        public GradientSignalResult(Signal signal, DateTime signalTime) : base(signal, signalTime)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}[iVal: {IndicatorValue}, bv: {BestValue} r: {ResistanceValue} t: {FirstValue} uv: {UsedValue}]"; ;
        }
    }

    public class GradientSignal : AnalyserBase
    {
        public ITechnicalIndicator Indicator { get; set; }
        public decimal ResistanceFirstAlfa { get; set; } = 0.015M;
        public decimal ResistanceNextAlfa { get; set; } = 0.005M;
        public decimal OutTolerance { get; set; } = 0.015M;

        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public decimal? FirstValue { get; set; }
        public decimal? BestValue { get; set; }
        public decimal? ResistanceValue { get; set; }
        public decimal? LastValue { get; set; }

        private FinanceList<decimal> criticalBars;


        public GradientSignal(string name, string symbol, AlgoBase owner, decimal l1, decimal l2) : base(name, symbol, owner)
        {
            L1 = l1;
            L2 = l2;
        }

        public override void Init()
        {            
            criticalBars = new FinanceList<decimal>(4);
            base.Init();
        }

        protected override void ResetInternal()
        {
            FirstValue = null;
            BestValue = null;
            LastValue = null;
            ResistanceValue = null;
            criticalBars.Clear();
            base.ResetInternal();
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new GradientSignalResult(this, Algo.Now);
            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            var outOfRange = false;

            if (CollectList.Ready && mp > 0)
            {
                decimal mpAverage = CollectList.LastValue;
                var iVal = Indicator.NextValue(mpAverage).Value.Value;
                result.IndicatorValue = iVal;

                AnalyseList.Collect(iVal);
                criticalBars.Push(iVal);
                
                if (AnalyseList.Ready)
                {
                    decimal currentValue = AnalyseList.LastValue;
                    BestValue = BestValue ?? currentValue;
                    ResistanceValue = ResistanceValue ?? currentValue;
                    result.UsedValue = currentValue;
                    if (currentValue > L1 && currentValue < L2)
                    {
                        FirstValue = FirstValue ?? currentValue;
                        result.LastValue = currentValue;

                        if (currentValue >= BestValue)
                        {
                            BestValue = currentValue;
                            ResistanceValue = currentValue - currentValue * ResistanceFirstAlfa;
                            Log($"Set best value: {BestValue} r: {ResistanceValue} c: {currentValue} f: {FirstValue}", LogLevel.Debug);

                        }
                        else if (currentValue <= ResistanceValue)
                        {
                            result.finalResult = BuySell.Sell;
                        }
                        else if (currentValue < BestValue)
                        {
                            var newResistance = ResistanceValue.Value + ResistanceValue.Value * ResistanceNextAlfa;
                            Log($"Increased resistance value: {ResistanceValue} -> {newResistance}. c: {currentValue} b: {BestValue} f: {FirstValue}", LogLevel.Debug);
                            ResistanceValue = newResistance;
                        } 
                        LastValue = currentValue;
                    }
                    else if (currentValue < L1 && currentValue > L2)
                    {
                        FirstValue = FirstValue ?? currentValue;
                        result.LastValue = currentValue;

                        if (currentValue <= BestValue)
                        {
                            BestValue = currentValue;
                            ResistanceValue = currentValue + currentValue * ResistanceFirstAlfa;
                            Log($"Set best value: {BestValue} r: {ResistanceValue} c: {currentValue} f: {FirstValue}", LogLevel.Debug);
                        }
                        else if (currentValue >= ResistanceValue)
                        {
                            result.finalResult = BuySell.Buy;
                        }
                        else if (currentValue > BestValue)
                        {
                            var newResistance = ResistanceValue.Value - ResistanceValue.Value * ResistanceNextAlfa;
                            Log($"Decreased resistance value: {ResistanceValue} -> {newResistance}. c: {currentValue} b: {BestValue} f: {FirstValue}", LogLevel.Debug);
                            ResistanceValue = newResistance;
                        }
                        LastValue = currentValue;
                    }
                    else
                    {
                        outOfRange = true;
                        if (FirstValue.HasValue)
                        {
                            var toleranceVal = FirstValue > L1 && FirstValue < L2 ? L1 - L1 * OutTolerance : L1 + L1 * OutTolerance;
                            if (toleranceVal < L1 && currentValue >= toleranceVal) result.finalResult = BuySell.Sell;
                            else if (toleranceVal > L1 && currentValue <= toleranceVal) result.finalResult = BuySell.Buy;
                            Log($"Fast increase/decrease detected.: {result.finalResult} {toleranceVal} {currentValue} {BestValue} {ResistanceValue}", LogLevel.Debug);

                        }
                    }
                }
            }
            result.L1 = L1;
            result.L2 = L2;
            result.BestValue = BestValue;            
            result.FirstValue = FirstValue;            
            result.ResistanceValue = ResistanceValue;
            result.BestValue = BestValue;
            if (result.finalResult.HasValue) ResetInternal();
            else if (outOfRange) ResetInternal();   
            //Log($"{result}", LogLevel.Warning);
            return result;
        }
    }
}
