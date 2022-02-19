// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Skender.Stock.Indicators;
using Kalitte.Trading.Indicators;
using Kalitte.Trading.Algos;

namespace Kalitte.Trading
{
    public enum TrendDirection
    {
        Up,
        Down,
        None
    }

    public class TrendSignalResult : SignalResultX
    {
        public TrendDirection Direction { get; set; }
        public decimal Change { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal LastBarValue { get; set; } = 0;


        public TrendSignalResult(Signal signal, TrendDirection direction, decimal value, decimal change, DateTime t) : base(signal, t)
        {
            this.CurrentValue = value;
            this.Change = change;
            this.Direction = direction;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | direction: {Direction} current: {CurrentValue} lastBar: {LastBarValue} change: {Change} ";
        }
    }

    public class TrendSignal : Signal
    {
        public ITradingIndicator i1k;
        public int PriceCollectionPeriod = 5;
        public int Periods = 5;
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }


        private FinanceBars analysisBars;
        private FinanceBars priceBars;
        private List<decimal> derivs;

        public bool UseSma = true;


        public TrendSignal(string name, string symbol, AlgoBase owner, decimal? min, decimal? max) : base(name, symbol, owner)
        {
            Min = min;
            Max = max;
        }

        public override void Init()
        {
            analysisBars = new FinanceBars(Periods);
            priceBars = new FinanceBars(PriceCollectionPeriod);
            derivs = new List<decimal>();
            i1k.InputBars.ListEvent += InputBars_ListEvent;
            generateDerivs();
        }

        private void InputBars_ListEvent(object sender, ListEventArgs<IQuote> e)
        {
            generateDerivs();
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Range: {Min}-{Max} Period: {Periods} PriceCollection: {PriceCollectionPeriod} useSma: {UseSma}";
        }


        private void generateDerivs()
        {
            derivs.Clear();
            var list = i1k.Results;
            for (var i = 0; i < list.Count; i++)
            {
                var index = i + 1;
                if (index >= list.Count) break;
                if (!list[index].HasValue || !list[i].HasValue) continue;
                derivs.Add(list[index].Value - list[i].Value);
            }
            if (derivs.Count > 1)
            {
                //var last2 = derivs[derivs.Count - 1] - derivs[derivs.Count - 2];
                //if (last2 > 0) lastDirection = TrendDirection.Up;
                //else if (last2 < 0) lastDirection = TrendDirection.Down;
                //else lastDirection = TrendDirection.None;
            }

            //Log($"generate list {list.Count} {(string.Join(sep, list.ToArray()))}", LogLevel.Info);
            //Log($"generate deriv {derivs.Count} {(string.Join(sep, derivs.ToArray()))}", LogLevel.Info);



        }

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var result = new TrendSignalResult(this, TrendDirection.None, 0, 0, t ?? DateTime.Now);

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) priceBars.Push(new Quote() { Date = t ?? DateTime.Now, Close = mp });


            if (priceBars.IsFull && mp >= 0)
            {

                decimal mpAverage = priceBars.List.GetSma(priceBars.Count).Last().Sma.Value;
                priceBars.Clear();

                var l1 = i1k.NextValue(mpAverage);

                var newResultBar = new Quote() { Date = t ?? DateTime.Now, Close = l1 };
                analysisBars.Push(newResultBar);

                if (analysisBars.Count >= Periods && derivs.Count > 0)
                {
                    var currentVal = UseSma ? analysisBars.List.GetSma(Periods).Last().Sma.Value : analysisBars.List.GetEma(Periods).Last().Ema.Value;
                    var lastBarData = i1k.Results.Last().Value;

                    result.CurrentValue = currentVal;
                    result.Change = currentVal - lastBarData;
                    result.LastBarValue = lastBarData;

                    var checkedLimits = !Min.HasValue && !Max.HasValue;

                    if (Min.HasValue && lastBarData <= Min.Value)
                    {
                        checkedLimits = true;
                    }
                    else if (Max.HasValue && lastBarData >= Max.Value)
                    {
                        checkedLimits = true;
                    }


                    if (checkedLimits)
                    {
                        if (result.Change < 0) result.Direction = TrendDirection.Down;
                        else if (result.Change > 0) result.Direction = TrendDirection.Up;
                    }

                    if (result.Direction != TrendDirection.None)
                    {
                        result.finalResult = result.Direction == TrendDirection.Up ? OrderSide.Buy : OrderSide.Sell;
                        analysisBars.Clear();
                    }

                    Log($"trend-signal: result: {result}", LogLevel.Verbose, t);


                   
                }
            }

            return result;

        }


    }
}
