// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Kalitte.Trading.Indicators
{



    public class VWMA : IndicatorBase<VwmaResult>
    {

  
        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public VWMA(FinanceBars bars, int periods, CandlePart candle = CandlePart.Close) : base(bars, candle)
        {
            this.Lookback = periods;
            CreateResult();
        }



        public override IEnumerable<VwmaResult> GetResults()
        {
            return UsedInput.GetVwma(Lookback);
        }

        protected override IndicatorResult ToValue(VwmaResult result)
        {
            return new IndicatorResult(result.Date, result.Vwma);
        }


        public override VwmaResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return list.GetVwma(this.Lookback).Last();
        }


    }
}
