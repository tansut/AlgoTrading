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


    public class Atr : IndicatorBase<AtrResult>
    {

        int startIndex = 0;

        public override string ToString()
        {
            return $"{base.ToString()}:({Periods})";
        }

        public Atr(FinanceBars bars, int periods) : base(bars)
        {
            this.Periods = periods;              
            createResult();
            this.InputBars.ListEvent += InputBars_BarEvent;
            startIndex = 0;
            
            
            
        }

        private void createResult()
        {
            ResultList.Clear();
            var results = LastBars.GetAtr(Periods).ToList();
            results.ForEach(r => ResultList.Push(r));
        }


        protected override IndicatorResult ToValue(AtrResult result)
        {
            return new IndicatorResult(result.Date, result.Atrp);
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
            get { return InputBars.LastItems(startIndex + 5 * Periods + 1); }
            //get { return InputBars.LastItems(InputBars.Count); }
        }

        public override decimal NextValue(decimal newVal)
        {
            var last = InputBars.Last;
            var q = new Quote() { Date = DateTime.Now, Low = last.Low, High = last.High, Close = newVal };
            return (decimal)(NextResult(q).Atrp ?? 0);

        }

        public override AtrResult NextResult(IQuote quote)
        {
            var list = LastBars;
            list.Add(quote);
            return list.GetAtr(Periods).Last();
        }
    }
}
