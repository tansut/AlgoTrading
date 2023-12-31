﻿// algo
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

        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Price(FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;              
            CreateResult();                               
        }


        public override IEnumerable<PriceResult> GetResults()
        {
            return UsedInput.Select(p => new PriceResult() { Price = p.Close, Date = p.Date });
        }

        protected override IndicatorResult ToValue(PriceResult result)
        {
            return new IndicatorResult(result.Date, result.Price);
        }





        public override PriceResult NextResult(IQuote quote)
        {
            return new PriceResult() { Price = quote.Close, Date = quote.Date };
        }
    }
}
