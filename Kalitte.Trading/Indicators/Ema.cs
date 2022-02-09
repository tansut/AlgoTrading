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
            bars.BarEvent += BarChanged;
            ResultBars = InputBars.Count < Periods ? new Bars(): createResult();
        }


        private Bars createResult()
        {
            ResultBars = new Bars();
            var result = InputBars.Ema(Periods);
            result.ForEach(r => ResultBars.Push(new Quote() { Date = r.Date, Close = r.Ema ?? 0 }));
            return ResultBars;
        }


        public override decimal NextValue(decimal newVal)
        {
            var lastEma = (double)(ResultBars.List.Last().Close);
            var ema = (decimal)InputBars.EmaNext((double)newVal, lastEma, Periods);
            return ema;
        }

        private void BarChanged(object sender, BarEvent e)
        {
            if (e.Action == BarActions.Cleared)
            {
                ResultBars = new Bars();

            } else if (HasResult)
            {
                if (e.Action == BarActions.BarCreated)
                {
                    var ema = new EmaResult() { Date = e.Item.Date };
                    var close = (double)(e.Item.Close);
                    var lastEma = (double)(ResultBars.List.Last().Close);
                    ema.Ema = (decimal)InputBars.EmaNext(close, lastEma, Periods);
                    ResultBars.Push(new Quote() { Date = ema.Date, Close = ema.Ema ?? 0 });
                }

                else if (e.Action == BarActions.BarRemoved)
                {
                    ResultBars = createResult();
                }
            }
            else ResultBars = createResult();
        }

        //public override List<decimal> Values
        //{
        //    get
        //    {
        //        return Bars.Ema(Periods);
        //    }
        //}

        //public override decimal LastValue(decimal newValue)
        //{
        //    return Bars.EmaNext(newValue, Bars.Ema(Periods).Last(), Periods);
        //}
    }
}
