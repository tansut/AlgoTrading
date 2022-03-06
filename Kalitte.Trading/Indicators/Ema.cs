// algo
using Kalitte.Trading.Matrix;
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
            createResult();
        }

        private void createResult()
        {
            ResultList.Clear();
            var result = UsedInput.GetEma(Lookback, this.Candle).ToList();
            result.ForEach(r => ResultList.Push(r));
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

        protected override void BarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            base.BarsChanged(sender, e);
            createResult();
        }



    }
}
