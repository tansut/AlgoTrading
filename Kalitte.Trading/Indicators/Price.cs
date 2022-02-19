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

    public class PriceResult: ResultBase
    {
        public decimal Price { get; set; }
    }

    public class Price : IndicatorBase<PriceResult>
    {

        int startIndex = 0;
        public override string ToString()
        {
            return $"{base.ToString()}:({Periods})";
        }

        public Price(FinanceBars bars, int periods) : base(bars)
        {
            this.Periods = periods;              
            createResult();
            this.InputBars.ListEvent += InputBars_BarEvent;
            startIndex = 0;
            
            
            
        }

        private void createResult()
        {
            ResultList.Clear();
            var results = LastBars.Select(p=>new PriceResult() { Price = p.Close, Date=p.Date}).ToList();
            results.ForEach(r => ResultList.Push(r));
        }


        protected override IndicatorResult ToValue(PriceResult result)
        {
            return new IndicatorResult(result.Date, result.Price);
        }


        private void InputBars_BarEvent(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared)
            {
                startIndex = 0;
            }

            else if (e.Action == ListAction.ItemAdded)
            {
                startIndex++;
            }

            else if (e.Action == ListAction.ItemRemoved)
            {
                startIndex--;
            }
            createResult(); 

        }

        public IList<IQuote> LastBars
        {
            get { return InputBars.LastItems(startIndex + Periods); }
        }

        public override decimal NextValue(decimal newVal)
        {
            return (decimal)(NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Price);

        }

        public override PriceResult NextResult(IQuote quote)
        {
            return new PriceResult() { Price = quote.Close, Date = quote.Date };
        }
    }
}
