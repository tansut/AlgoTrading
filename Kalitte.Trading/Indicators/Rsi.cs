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

        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Rsi(FinanceBars bars, int periods, CandlePart candle = CandlePart.Close) : base(bars, candle)
        {
            this.Lookback = periods;              
            createResult();       
        }

        private void createResult()
        {
            ResultList.Clear();
            var results = UsedInput.GetRsi(Lookback).ToList();
            results.ForEach(r => ResultList.Push(r));
        }


        protected override IndicatorResult ToValue(RsiResult result)
        {
            return new IndicatorResult(result.Date, (decimal?)result.Rsi);
        }

        protected override void BarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared) UsedInput.Clear();
            else if (e.Action == ListAction.ItemRemoved)
            {
                CreateUsedBars();
            }
            else if (e.Action == ListAction.ItemAdded)
            {
                if (Candle != CandlePart.Close)
                {
                    var item = new MyQuote() { Close = e.Item.Volume, Date = e.Item.Date };
                    UsedInput.Add(item);
                }
                else UsedInput.Add(e.Item);
            }
            createResult();
        }


        protected override List<IQuote> CreateUsedBars()
        {            
            var skip = LastItems;
            if (Candle != CandlePart.Close)
            {
                var list = InputBars.LastItems(skip);
                var result = new List<MyQuote>();
                list.ForEach(p => result.Add(new MyQuote() {  Date = p.Date, Close = p.Volume}));
                return result.ToList<IQuote>();
                //var items = InputBars.Values(this.Candle).Skip(Math.Max(0, InputBars.Count - skip));                

            } else return InputBars.LastItems(skip);
        }

        


        public override decimal NextValue(decimal newVal)
        {
            return (decimal)(NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Rsi ?? 0);

        }

        public override RsiResult NextResult(IQuote quote)
        {
            var list = this.UsedInput.ToList();
            list.Add(quote);
            return list.GetRsi(Lookback).Last();
        }
    }
}
