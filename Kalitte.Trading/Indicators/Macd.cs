// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{
    public class Macd : TradingIndicator<MacdResult>
    {
        public int Short { get; set; }
        public int Long { get; set; }
        public int Signal { get; set; }

  
        public Ema EMa1 { get; set; }
        public Ema EMa2 { get; set; }
        public Ema Trigger { get; set; }


        public Macd(FinanceBars bars, int shortp, int longp, int signal) : base(bars)
        {
            this.Short = shortp;
            this.Long = longp;
            this.Signal = signal;
            this.EMa1 = new Ema(InputBars, shortp);
            this.EMa2 = new Ema(InputBars, longp);                       
            if (InputBars.Count >= longp) createResult();
            this.InputBars.ListEvent += InputBars_BarEvent;
            //this.Trigger = new Ema(Results, signal);
        }

        private void createResult()
        {
            Results.Clear();
            for (var i = 0; i < EMa1.Results.Count; i++)
            {
                var data1 = EMa1.Results.List;
                var data2 = EMa2.Results.List;
                Results.Push(new MacdResult() { 
                    Date = data1[i].Date, 
                    FastEma = data1[i].Ema, SlowEma = data2[i].Ema, Macd = data1[i].Ema.HasValue && data2[i].Ema.HasValue ? data1[i].Ema - data2[i].Ema: null  });
            }

        }

        public bool HasResult => Results.Count > 0 && Results.Last.FastEma.HasValue && Results.Last.SlowEma.HasValue;


        private void InputBars_BarEvent(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared)
            {
                Results.Clear();
            }
            else if (HasResult)
            {
                if (e.Action == ListAction.ItemAdded && EMa2.HasResult)
                {
                    Results.Push(new MacdResult()
                    {
                        Date = EMa1.Results.Last.Date,
                        Macd = EMa1.Results.Last.Ema - EMa2.Results.Last.Ema
                    });
                }

                else if (e.Action == ListAction.ItemRemoved)
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
    }
}
