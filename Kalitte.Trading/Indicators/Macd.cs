﻿// algo
using Kalitte.Trading.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace Kalitte.Trading.Indicators
{

    public class MacdTrigger : IndicatorBase<EmaResult>
    {
        public Macd Owner;

        public override decimal NextValue(decimal newVal)
        {
            return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Ema ?? 0;

        }

        private void createResult()
        {
            ResultList.Clear();
            var result = Owner.ResultList.List.Select(p => new EmaResult() { Date = p.Date, Ema = p.Signal }).ToList();
            result.ForEach(r => ResultList.Push(r));
        }

        public MacdTrigger(Macd owner) : base(owner.InputBars)
        {
            Owner = owner;
            createResult();
            Owner.ResultList.ListEvent += Bars_ListEvent;
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

        protected override IndicatorResult ToValue(EmaResult result)
        {
            return new IndicatorResult(result.Date, result.Ema);

        }
    }

    public class Macd : IndicatorBase<MacdResult>
    {
        public int Slow { get; set; }
        public int Fast { get; set; }
        public int Signal { get; set; }

        int startIndex = 0;

        public MacdTrigger Trigger { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}:({Slow}, {Fast}, {Signal})";

        }


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
            ResultList.Clear();
            var results = LastBars.GetMacd(this.Fast, this.Slow, this.Signal).ToList();
            results.ForEach(r => ResultList.Push(r));            
        }



        //public bool HasResult => Results.Count > 0 && Results.Last.FastEma.HasValue && Results.Last.SlowEma.HasValue;


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
            get { return InputBars.LastItems(Signal + Fast + Slow + startIndex); }
            //get { return InputBars.List; }
        }

        public override decimal NextValue(decimal newVal)
        {
            return NextResult(new Quote() { Date = DateTime.Now, Close = newVal }).Macd ?? 0;
        }

        protected override IndicatorResult ToValue(MacdResult result)
        {
            return new IndicatorResult(result.Date, result.Macd);
        }


        public override MacdResult NextResult(IQuote quote)
        {
            var list = LastBars;
            list.Add(quote);
            return list.GetMacd(this.Fast, this.Slow, this.Signal).Last();
        }
    }
}
