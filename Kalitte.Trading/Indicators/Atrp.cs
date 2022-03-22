// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{


    public class Atrp : IndicatorBase<AtrResult>
    {


        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Atrp(FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;              
            CreateResult();            
        }



        public override IEnumerable<AtrResult> GetResults()
        {
            return UsedInput.GetAtr(Lookback);
        }


        protected override IndicatorResult ToValue(AtrResult result)
        {
            return new IndicatorResult(result.Date, result.Atrp);
        }




        protected override List<IQuote> CreateUsedBars()
        {
            return  InputBars.LastItems(5 * Lookback + 1);
        }


        public override IndicatorResult NextValue(decimal? price = null, decimal? volume = null)
        {
            var last = UsedInput.Last();
            var q = new Quote() { Date = DateTime.Now, Low = last.Low, High = last.High, Close = price.Value };
            var r = NextResult(q);
            return new IndicatorResult(DateTime.Now, (decimal)(r.Atrp ?? 0));

        }

        public override AtrResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return list.GetAtr(Lookback).Last();
        }
    }
}
