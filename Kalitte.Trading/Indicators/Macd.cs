// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{
    public class Macd : IndicatorBase
    {
        public int Short { get; set; }
        public int Long { get; set; }
        public int Signal { get; set; }

        public Ema Trigger { get; set; }

        public Macd(Bars bars, int shortp, int longp, int signal) : base(bars)
        {
            this.Short = shortp;
            this.Long = longp;
            this.Signal = signal;
            this.Trigger = new Ema(Bars, signal);
        }

        public override decimal LastValue(decimal newValue)
        {
            lock(this)
            {
                var em1 = Bars.Ema(Short);
                var em2 = Bars.Ema(Long);
                var difs = new Bars();
                var data = Bars.List;
                for (int i = 0; i < em1.Count; i++) difs.Push(new Quote(data[i].Date, em1[i] - em2[i]));
                return Bars.EmaNext(newValue, difs.Ema(Long).Last(), Long);
                //return difs.Ema(Signal)

            }
            //return Bars.EmaNext(newValue, Bars.Ema(Periods).Last(), Periods);
        }
    }
}
