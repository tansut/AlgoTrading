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

    public class VolumeResult : ResultBase
    {
        public decimal Volume { get; set; }
    }

    public class Volume : IndicatorBase<VolumeResult>
    {

        public FinanceList<IQuote> Bars { get; private set; }
        public int SliceSeconds { get; set; } = 0;
        public int EMAPeriod { get; set; }



        public override string ToString()
        {
            return $"{base.ToString()}:Lookback: {Lookback}";
        }

        public Volume(FinanceBars bars, int lookback, int sliceBySeconds) : base(bars)
        {
            this.Lookback = lookback;
            this.SliceSeconds = sliceBySeconds;
            this.Bars = new FinanceList<IQuote>(lookback * 2, bars.LastItems(lookback));
            Bars.ListEvent += Bars_ListEvent;
            createResult();
        }

        private void Bars_ListEvent(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared) createResult();
            else if (e.Action == ListAction.ItemRemoved) return;
            Helper.SymbolSeconds(InputBars.Period.ToString(), out int periodSeconds);
            var sliceVal = periodSeconds / SliceSeconds;
            var avgVols = e.Item.Volume / sliceVal;
            var res = new VolumeResult()
            {
                Date = e.Item.Date,
                Volume = (decimal)FinanceBars.EmaNext((double)avgVols, (double)ResultList.Last.Volume, Lookback)
            };

            ResultList.Push(res);
        }

        private decimal calculatePower(decimal oldVal, decimal newVal)
        {
            return newVal - oldVal / newVal;
        }

        private void createResult()
        {
            ResultList.Clear();
            var input = Bars.AsList;
            var results = new List<IQuote>();

            Helper.SymbolSeconds(InputBars.Period.ToString(), out int periodSeconds);
            var sliceVal = periodSeconds / SliceSeconds;

            for (var i = 0; i < input.Count; i++)
            {
                var avgVols = input[i].Volume / sliceVal;
                var res = new MyQuote()
                {
                    Date = input[i].Date,
                    Volume = avgVols
                };
                results.Add(res);
            }
            var emas = results.GetEma(Lookback, CandlePart.Volume).ToList();
            emas.ForEach(e =>
            {
                if (e.Ema.HasValue)
                    ResultList.Push(new VolumeResult() { Volume = e.Ema.Value });
            });
        }



        protected override IndicatorResult ToValue(VolumeResult result)
        {
            return new IndicatorResult(result.Date, result.Volume);
        }



        public override decimal NextValue(decimal newVal)
        {
            return (decimal)(NextResult(new Quote() { Date = DateTime.Now, Volume = newVal }).Volume);
        }

        public override VolumeResult NextResult(IQuote quote)
        {
            var lastEma = (double)(ResultList.Last.Volume);
            var ema = (decimal)FinanceBars.EmaNext((double)quote.Volume, lastEma, Lookback);
            return new VolumeResult() { Volume = ema, Date = quote.Date };
        }
    }
}
