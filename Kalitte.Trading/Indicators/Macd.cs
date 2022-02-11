// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{

    public class MacdTrigger : TradingIndicator<EmaResult>
    {
        public Macd Owner;

        public override decimal NextValue(decimal newVal)
        {
            return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Ema ?? 0;

        }

        private void createResult()
        {
            Results.Clear();
            var result = Owner.Results.List.Select(p => new EmaResult() { Date = p.Date, Ema = p.Signal }).ToList();
            result.ForEach(r => Results.Push(r));
        }

        public MacdTrigger(Macd owner): base(owner.InputBars)
        {
            Owner = owner;
            Results = new FinanceList<EmaResult>();
            Owner.Results.ListEvent += Bars_ListEvent;
        }

        private void Bars_ListEvent(object sender, ListEventArgs<MacdResult> e)
        {
            createResult();
        }

        public override EmaResult NextResult(IQuote quote)
        {
            var next = Owner.NextResult(quote);
            return new EmaResult() { Date = quote.Date, Ema = next.Signal };

        }
    }

    public class Macd : TradingIndicator<MacdResult>
    {
        public int Slow { get; set; }
        public int Fast { get; set; }
        public int Signal { get; set; }

        int startIndex = 0;

        public MacdTrigger Trigger { get; set; }


        public Macd(FinanceBars bars, int fast, int slow, int signal) : base(bars)
        {
            this.Slow = slow;
            this.Fast = fast;
            this.Signal = signal;               
            createResult();
            this.InputBars.ListEvent += InputBars_BarEvent;
            this.Trigger = new MacdTrigger(this);
            startIndex = 0;
        }

        private void createResult()
        {
            Results.Clear();
            var results = LastBars.GetMacd(this.Fast, this.Slow, this.Signal).ToList();
            results.ForEach(r => Results.Push(r));
        }



        public bool HasResult => Results.Count > 0 && Results.Last.FastEma.HasValue && Results.Last.SlowEma.HasValue;


        private void InputBars_BarEvent(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared)
            {
                startIndex = 0;
                Results.Clear();
            }
            else if (HasResult)
            {
                if (e.Action == ListAction.ItemAdded)
                {
                    startIndex++;
                    createResult();
                }

                else if (e.Action == ListAction.ItemRemoved)
                {
                    startIndex--;
                    createResult();
                }
            }
            else createResult();
        }

        public IList<IQuote> LastBars
        {
            get { return InputBars.LastItems(Signal+Slow+startIndex); }
        }

        public override decimal NextValue(decimal newVal)
        {
            return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Macd ?? 0;

        }

        public override MacdResult NextResult(IQuote quote)
        {
            var list = LastBars;
            list.Add(quote);
            return list.GetMacd(this.Fast, this.Slow, this.Signal).Last();
        }
    }
}
