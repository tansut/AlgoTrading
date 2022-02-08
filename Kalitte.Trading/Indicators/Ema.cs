// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{
    public class Ema : IndicatorBase
    {
        public int Periods { get; set; }

        public Ema(Bars bars, int periods) : base(bars)
        {
            this.Periods = periods;
        }

        public override List<decimal> Values
        {
            get
            {
                return Bars.Ema(Periods);
            }
        }

        public override decimal LastValue(decimal newValue)
        {
            return Bars.EmaNext(newValue, Bars.Ema(Periods).Last(), Periods);
        }
    }
}
