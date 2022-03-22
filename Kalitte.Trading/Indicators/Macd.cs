// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{

    public class MacdTrigger : IndicatorBase<EmaResult>
    {
        public Macd Owner;


        public override IEnumerable<EmaResult> GetResults()
        {
            return Owner.ResultList.List.Select(p => new EmaResult() { Date = p.Date, Ema = p.Signal });
        }

        public MacdTrigger(Macd owner) : base(owner.InputBars)
        {
            Owner = owner;
            CreateResult();
            Owner.ResultList.ListEvent += Bars_ListEvent;
        }

        


        private void Bars_ListEvent(object sender, ListEventArgs<MacdResult> e)
        {
            CreateResult();
        }

        public override EmaResult NextResult(IQuote quote)
        {
            var next = Owner.NextResult(quote);
            return new EmaResult() { Date = quote.Date, Ema = next.Signal };

        }

        protected override IndicatorResult ToValue(EmaResult result)
        {
            return new IndicatorResult(result.Date, result.Ema);

        }
    }

    public class Macd : IndicatorBase<MacdResult>
    {
        public int Slow { get; set; }
        public int Fast { get; set; }
        public int Signal { get; set; }

        public MacdTrigger Trigger { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}:({Slow}, {Fast}, {Signal})";

        }


        public Macd(FinanceBars bars, int fast, int slow, int signal) : base(bars)
        {
            this.Slow = slow;
            this.Fast = fast;
            this.Signal = signal;
            CreateResult();
            this.Trigger = new MacdTrigger(this);
        }



        public override IEnumerable<MacdResult> GetResults()
        {
            return UsedInput.GetMacd(this.Fast, this.Slow, this.Signal);
        }




        //protected override List<IQuote> CreateUsedBars()
        //{
        //    return InputBars.LastItems(Math.Min(InputBars.Count, 2 * (Signal + Fast + Slow)));
        //    //return InputBars.AsList;
        //}

        //public override decimal NextValue(decimal newVal)
        //{
        //    return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Macd ?? 0;
        //}

        protected override IndicatorResult ToValue(MacdResult result)
        {
            return new IndicatorResult(result.Date, result.Macd);
        }


        public override MacdResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return list.GetMacd(this.Fast, this.Slow, this.Signal).Last();
        }
    }
}
