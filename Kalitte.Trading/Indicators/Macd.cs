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

  
        public Ema EMa1 { get; set; }
        public Ema EMa2 { get; set; }
        public Ema Trigger { get; set; }


        public Macd(Bars bars, int shortp, int longp, int signal) : base(bars)
        {
            this.Short = shortp;
            this.Long = longp;
            this.Signal = signal;
            this.EMa1 = new Ema(InputBars, shortp);
            this.EMa2 = new Ema(InputBars, longp);
            
            ResultBars = new Bars();
            if (InputBars.Count >= longp) createResult();
            this.InputBars.BarEvent += InputBars_BarEvent;



            this.Trigger = new Ema(ResultBars, signal);
        }

        private Bars createResult()
        {
            ResultBars.Clear();
            for (var i = 0; i < EMa1.ResultBars.Count; i++)
            {
                var data1 = EMa1.ResultBars.List;
                var data2 = EMa2.ResultBars.List;
                ResultBars.Push(new Quote(data1[i].Date, data1[i].Close - data2[i].Close));
            }

            return ResultBars;
        }

        private void InputBars_BarEvent(object sender, BarEvent e)
        {
            if (e.Action == BarActions.Cleared)
            {
                ResultBars.Clear();

            }
            else if (HasResult)
            {
                if (e.Action == BarActions.BarCreated && EMa2.HasResult)
                {
                    ResultBars.Push(new Quote()
                    {
                        Date = EMa1.ResultBars.Latest.Date,
                        Close = (EMa1.ResultBars.Latest.Close - EMa2.ResultBars.Latest.Close)
                    });
                }

                else if (e.Action == BarActions.BarRemoved)
                {
                    createResult();
                }
            }
            else createResult();
        }

        public override decimal NextValue(decimal newVal)
        {
            var em1 = EMa1.NextValue(newVal);
            var em2 = EMa2.NextValue(newVal);
            return em1 - em2;
        }



        //public override decimal LastValue(decimal newValue)
        //{
        //    lock(this)
        //    {
        //        var em1 = Bars.Ema(Short);
        //        var em2 = Bars.Ema(Long);
        //        var difs = new Bars();
        //        var data = Bars.List;
        //        for (int i = 0; i < em1.Count; i++) difs.Push(new Quote(data[i].Date, em1[i] - em2[i]));
        //        return Bars.EmaNext(newValue, difs.Ema(Long).Last(), Long);
        //        //return difs.Ema(Signal)

        //    }
        //    //return Bars.EmaNext(newValue, Bars.Ema(Periods).Last(), Periods);
        //}
    }
}
