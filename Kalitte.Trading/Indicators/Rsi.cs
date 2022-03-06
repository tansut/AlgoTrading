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
            base.CreateResult();
        }


        public override IEnumerable<RsiResult> GetResults()
        {
            return UsedInput.GetRsi(Lookback);
        }


        protected override IndicatorResult ToValue(RsiResult result)
        {
            return new IndicatorResult(result.Date, (decimal?)result.Rsi);
        }

        protected override void BarsChangedEvent(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared) base.BarsChangedEvent(sender, e);
            else if (e.Action == ListAction.ItemRemoved) base.BarsChangedEvent(sender, e);
            else if (e.Action == ListAction.ItemAdded)
            {
                if (Candle != CandlePart.Close)
                {
                    ResultList.WriterLock();
                    try
                    {
                        var item = new MyQuote() { Close = e.Item.Volume, Date = e.Item.Date };
                        UsedInput.Add(item);
                        base.CreateResult();
                    }
                    finally
                    {
                        ResultList.RelaseWriter();
                    }
                }
                else base.BarsChangedEvent(sender, e);
            }
        }


        protected override List<IQuote> CreateUsedBars()
        {
            if (Candle != CandlePart.Close)
            {
                var list = InputBars.RecommendedItems;
                var result = new List<MyQuote>();
                list.ForEach(p => result.Add(new MyQuote() { Date = p.Date, Close = p.Volume }));
                return result.ToList<IQuote>();

            }
            else return base.CreateUsedBars();
        }


        public override RsiResult NextResult(IQuote quote)
        {
            var list = this.UsedInput.ToList();
            list.Add(quote);
            return list.GetRsi(Lookback).Last();
        }
    }
}
