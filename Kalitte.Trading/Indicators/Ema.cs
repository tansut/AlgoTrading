// algo
using Kalitte.Trading.Algos;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{



    public class Ema : TradingIndicator<EmaResult>
    {
        public int Periods { get; set; }
        

        public Ema(FinanceBars bars, int periods) : base(bars)
        {
            this.Periods = periods;

            bars.ListEvent += BarChanged;            
            if (InputBars.Count >= Periods)  createResult();
        }

        public bool HasResult => Results.Count > 0 && Results.Last.Ema.HasValue;


        private void createResult()
        {
            Results.Clear();
            var result = InputBars.Ema(Periods);
            result.ForEach(r => Results.Push(new EmaResult() {  Date = r.Date, Ema = r.Ema}));
        }


        public override decimal NextValue(decimal newVal)
        {
            var lastEma = (double)(Results.Last.Ema);
            var ema = (decimal)InputBars.EmaNext((double)newVal, lastEma, Periods);
            return ema;
        }

        private void BarChanged(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared)
            {
                Results.Clear();

            } else if (HasResult)
            {
                if (e.Action == ListAction.ItemAdded)
                {
                    var ema = new EmaResult() { Date = e.Item.Date };
                    var close = (double)(e.Item.Close);
                    var lastEma = (double)(Results.Last.Ema);
                    ema.Ema = (decimal)InputBars.EmaNext(close, lastEma, Periods);
                    Results.Push(new EmaResult() { Date = ema.Date, Ema = ema.Ema });
                }

                else if (e.Action == ListAction.ItemRemoved)
                {
                    createResult();
                }
            }
            else createResult();
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
