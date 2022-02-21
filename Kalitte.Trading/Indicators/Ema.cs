﻿// algo
using Kalitte.Trading.Matrix;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Kalitte.Trading.Indicators
{



    public class Ema : IndicatorBase<EmaResult>
    {


        public override string ToString()
        {
            return $"{base.ToString()}:({Lookback})";
        }

        public Ema(FinanceBars bars, int periods) : base(bars)
        {
            this.Lookback = periods;
            bars.ListEvent += BarChanged;
            createResult();
        }

        public bool HasResult => ResultList.Count > 0 && ResultList.Last.Ema.HasValue;



        private void createResult()
        {
            ResultList.Clear();
            var result = LastBars.GetEma(Lookback).ToList();
            result.ForEach(r => ResultList.Push(r));
        }

        protected override IndicatorResult ToValue(EmaResult result)
        {
            return new IndicatorResult(result.Date, result.Ema);
        }

        public IList<IQuote> LastBars
        {
            get { return InputBars.LastItems(Lookback); }
        }



        public override decimal NextValue(decimal newVal)
        {
            var lastEma = (double)(ResultList.Last.Ema);
            var ema = (decimal)FinanceBars.EmaNext((double)newVal, lastEma, Lookback);
            return ema;
        }

        public override EmaResult NextResult(IQuote quote)
        {
            var lastEma = (double)(ResultList.Last.Ema);
            var ema = (decimal)FinanceBars.EmaNext((double)quote.Close, lastEma, Lookback);
            return new EmaResult() { Date = quote.Date, Ema = ema };
        }

        private void BarChanged(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared)
            {
                ResultList.Clear();

            }
            else if (HasResult)
            {
                if (e.Action == ListAction.ItemAdded)
                {
                    //List<IQuote> temp = new List<IQuote>(Periods);
                    //for(int i=0; i < Periods;i++)
                    //{
                    //    temp.Add(new Quote() { Date = Results.Last.Date, Close = Results.Last.Ema.Value });
                    //}
                    //temp.Add(e.Item);
                    //var lastResult = temp.GetEma(Periods);

                    var ema = new EmaResult() { Date = e.Item.Date };
                    var close = (double)(e.Item.Close);
                    var lastEma = (double)(ResultList.Last.Ema);
                    ema.Ema = (decimal)FinanceBars.EmaNext(close, lastEma, Lookback);
                    ResultList.Push(new EmaResult() { Date = ema.Date, Ema = ema.Ema });
                    //Results.Push(lastResult.Last());

                }

                else if (e.Action == ListAction.ItemRemoved)
                {
                    createResult();
                }
            }
            else createResult();
        }


    }
}
