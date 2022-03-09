// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class GradientResult
    {
        public BuySell? FinalResult { get; set; }
        public decimal? BestValue { get; set; }
        public decimal? FirstValue { get; set; }
        public decimal? LastValue { get; set; }
        public decimal? ResistanceValue { get; set; }
        public decimal? UsedValue { get; set; }
        public bool OutOfRange { get; set; } = false;


        public override string ToString()
        {
            return $"bv: {BestValue} r: {ResistanceValue} t: {FirstValue} uv: {UsedValue}"; 
        }

    }

    public class Gradient
    {
        public decimal ResistanceFirstAlfa { get; set; } = 0.015M;
        public decimal ResistanceNextAlfa { get; set; } = 0.005M;
        public decimal OutTolerance { get; set; } = 0.015M;

        public decimal L1 { get; set; }
        public decimal L2 { get; set; }
        public decimal? FirstValue { get; set; }
        public decimal? BestValue { get; set; }
        public decimal? ResistanceValue { get; set; }
        public decimal? LastValue { get; set; }

        public ILogProvider LogProvider { get; set; }

        public void Reset()
        {
            FirstValue = null;
            BestValue = null;
            LastValue = null;
            ResistanceValue = null;
        }


        public Gradient(decimal l1, decimal l2, ILogProvider logProvider)
        {
            this.L1 = l1;
            this.L2 = l2;
            this.LogProvider = logProvider;
        }

        public GradientResult Step(decimal currentValue)
        {
            var result = new GradientResult();
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
                    LogProvider.Log($"Set best value: {BestValue} r: {ResistanceValue} c: {currentValue} f: {FirstValue}", LogLevel.Debug);

                }
                else if (currentValue <= ResistanceValue)
                {
                    result.FinalResult = BuySell.Sell;
                }
                else if (currentValue < BestValue)
                {
                    var newResistance = ResistanceValue.Value + ResistanceValue.Value * ResistanceNextAlfa;
                    LogProvider.Log($"Increased resistance value: {ResistanceValue} -> {newResistance}. c: {currentValue} b: {BestValue} f: {FirstValue}", LogLevel.Debug);
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
                    LogProvider.Log($"Set best value: {BestValue} r: {ResistanceValue} c: {currentValue} f: {FirstValue}", LogLevel.Debug);
                }
                else if (currentValue >= ResistanceValue)
                {
                    result.FinalResult = BuySell.Buy;
                }
                else if (currentValue > BestValue)
                {
                    var newResistance = ResistanceValue.Value - ResistanceValue.Value * ResistanceNextAlfa;
                    LogProvider.Log($"Decreased resistance value: {ResistanceValue} -> {newResistance}. c: {currentValue} b: {BestValue} f: {FirstValue}", LogLevel.Debug);
                    ResistanceValue = newResistance;
                }
                LastValue = currentValue;
            }
            else
            {
                result.OutOfRange = true;
                if (FirstValue.HasValue)
                {
                    var toleranceVal = FirstValue > L1 && FirstValue < L2 ? L1 - L1 * OutTolerance : L1 + L1 * OutTolerance;
                    if (toleranceVal < L1 && currentValue >= toleranceVal) result.FinalResult = BuySell.Sell;
                    else if (toleranceVal > L1 && currentValue <= toleranceVal) result.FinalResult = BuySell.Buy;
                    LogProvider.Log($"Fast increase/decrease detected.: {result.FinalResult} {toleranceVal} {currentValue} {BestValue} {ResistanceValue}", LogLevel.Debug);

                }
            }
            return result;
        }
    }
}
