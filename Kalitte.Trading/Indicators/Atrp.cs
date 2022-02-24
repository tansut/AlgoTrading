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


    public class Atrp : IndicatorBase<AtrResult>
    {


        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Atrp(FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;              
            createResult();            
        }

        private void createResult()
        {
            ResultList.Clear();
            var results = UsedInput.GetAtr(Lookback).ToList();
            results.ForEach(r => ResultList.Push(r));
        }


        protected override IndicatorResult ToValue(AtrResult result)
        {
            return new IndicatorResult(result.Date, result.Atrp);
        }


        protected override void BarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            base.BarsChanged(sender, e);
            createResult();
        }

        protected override List<IQuote> CreateUsedBars()
        {
            return  InputBars.LastItems(5 * Lookback + 1);
        }


        public override decimal NextValue(decimal newVal)
        {
            var last = UsedInput.Last();
            var q = new Quote() { Date = DateTime.Now, Low = last.Low, High = last.High, Close = newVal };
            var r = NextResult(q);
            return (decimal)(r.Atrp ?? 0);

        }

        public override AtrResult NextResult(IQuote quote)
        {
            var list = UsedInput.ToList();
            list.Add(quote);
            return list.GetAtr(Lookback).Last();
        }
    }
}
