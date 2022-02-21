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


    public class Rsi : IndicatorBase<RsiResult>
    {

        int startIndex = 0;

        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Rsi(FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;              
            createResult();
            this.InputBars.ListEvent += InputBars_BarEvent;
            startIndex = 0;            
        }

        private void createResult()
        {
            ResultList.Clear();
            var results = LastBars.GetRsi(Lookback).ToList();
            results.ForEach(r => ResultList.Push(r));
        }


        protected override IndicatorResult ToValue(RsiResult result)
        {
            return new IndicatorResult(result.Date, (decimal?)result.Rsi);
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
            get { return InputBars.LastItems(startIndex + 2 * Lookback + 1); }
        }

        public override decimal NextValue(decimal newVal)
        {
            return (decimal)(NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Rsi ?? 0);

        }

        public override RsiResult NextResult(IQuote quote)
        {
            var list = LastBars;
            list.Add(quote);
            return list.GetRsi(Lookback).Last();
        }
    }
}
