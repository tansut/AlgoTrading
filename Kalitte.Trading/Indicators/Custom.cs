// algo
using Kalitte.Trading.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{
    public class CustomResult : ResultBase
    {
        public decimal Value { get; set; }
    }

    public class Custom: IndicatorBase<CustomResult>
    {

 
        Func<IQuote, decimal> Func;

        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Custom(Func<IQuote, decimal> func, FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;
            this.Func = func;
            createResult();     
        }

        private void createResult()
        {
            ResultList.Clear();
            foreach (var iten in UsedInput)
                ResultList.Push(new CustomResult() { Date = iten.Date, Value = Func(iten) });

        }


        protected override IndicatorResult ToValue(CustomResult result)
        {
            return new IndicatorResult(result.Date, (decimal?)result.Value);
        }


        protected override void BarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            base.BarsChanged(sender, e);
            createResult();
        }


        public override decimal NextValue(decimal newVal)
        {
            return (decimal)(NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Value);
        }

        public override CustomResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return new CustomResult() { Date = quote.Date, Value = Func(quote) };
        }
    }
}
