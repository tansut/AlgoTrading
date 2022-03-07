using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public enum Trend
    {
        Up,
        Down
    }

    

    public class FibonacciResult
    {
        public FibonacciResult(decimal ratio, decimal value)
        {
            Ratio = ratio;
            Value = value;
        }        
        public decimal Ratio { get; set; }
        public decimal Value { get; set; }
    }

    public class FibonacciList: List<FibonacciResult>
    {
        public FibonacciResult Get(decimal ratio)
        {
            return this.Where(x => x.Ratio == ratio).FirstOrDefault();
        }


    }

    public class Fibonacci
    {
        public decimal Low { get; set; }
        public decimal High { get; set; }

        public FibonacciList UpRetracement { get; private set; } = new FibonacciList();
        public FibonacciList DownRetracement { get; private set; } = new FibonacciList();
        public FibonacciList UpExtension { get; private set; } = new FibonacciList();
        public FibonacciList DownExtension { get; private set; } = new FibonacciList();


        public static decimal[] RetracementRatios = new decimal[] { 0.0M, 23.6M, 38.2M, 50, 61.8M, 78.6M, 100, 138.2M };
        public static decimal[] ExtensionRatios = new decimal[] { 261.8M, 200, 161.8M, 138.2M, 100.0M, 61.8M };

        public Fibonacci(decimal low, decimal high)
        {
            this.Low = low;
            this.High = high;
            calculate();
        }

        private void calculate()
        {
            foreach (var r in RetracementRatios)
            {
                UpRetracement.Add(new FibonacciResult(r, High - ((High - Low) * (r / 100M))));                
            }
            foreach (var r in ExtensionRatios)
            {
                UpExtension.Add(new FibonacciResult(r, High + ((High - Low) * (r / 100M))));
            }
            for (var i = RetracementRatios.Length - 1; i >= 0; i--)
            {
                var r = RetracementRatios[i];
                DownRetracement.Add(new FibonacciResult(r, Low + ((High - Low) * (r / 100M))));
            }
            for (var i = ExtensionRatios.Length - 1; i >= 0; i-- )
            {
                var r = ExtensionRatios[i];
                DownExtension.Add(new FibonacciResult(r, Low - ((High - Low) * (r / 100M))));
            }
        }
    }
}
