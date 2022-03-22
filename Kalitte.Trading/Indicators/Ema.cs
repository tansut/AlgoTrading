// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Kalitte.Trading.Indicators
{



    public class Ema : IndicatorBase<EmaResult>
    {

  
        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Ema(FinanceBars bars, int periods, CandlePart candle = CandlePart.Close) : base(bars, candle)
        {
            this.Lookback = periods;
            CreateResult();
        }



        public override IEnumerable<EmaResult> GetResults()
        {
            return UsedInput.GetEma(Lookback, this.Candle);
        }

        protected override IndicatorResult ToValue(EmaResult result)
        {
            return new IndicatorResult(result.Date, result.Ema);
        }


        //public override decimal NextValue(decimal newVal)
        //{
        //    return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Ema ?? 0;
        //}

        public override EmaResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return list.GetEma(this.Lookback, this.Candle).Last();
        }


    }
}
