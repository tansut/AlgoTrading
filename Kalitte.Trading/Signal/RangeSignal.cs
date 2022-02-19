// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
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
    public enum RangeStatus
    {
        BelowMin,
        InRange,
        AboveHigh
    }

    public class RangeSignalResult : SignalResultX
    {
        public RangeStatus? Status { get; set; }
        public decimal Value { get; set; }

        public RangeSignalResult(Signal signal, RangeStatus? status, DateTime t) : base(signal, t)
        {
            this.Status = status;
        }
    }

    public class RangeSignal : Signal
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public int AnalysisPeriod { get; set; } = 3;
        public IIndicator i1k;

        FinanceBars bars;

        public RangeSignal(string name, string symbol, AlgoBase owner, 
            decimal? min, decimal? max) : base(name, symbol, owner)
        {
            Min = min;
            Max = max;
        }

        public override void Init()
        {
            bars = new FinanceBars(AnalysisPeriod);
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {Min}-{Max} range, period: {AnalysisPeriod}";
        }

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            BuySell? result = null;
            RangeStatus? status = null;

            var mp = Algo.GetMarketPrice(Symbol, t);

            //if (mp == 0)
            //{
            //    mp = i1k.InputBars.Last.Close;
            //    Log($"Used last close bar price { mp } since market price is unavailable.", LogLevel.Warning, t);
            //}

            if (i1k.CurrentValue.HasValue)
            {
                bars.Push(new MyQuote() { Date = DateTime.Now, Close = i1k.NextValue(mp) });
            }
            decimal value = 0;

            if (bars.Count >= AnalysisPeriod)
            {
                var val = bars.List.GetSma(AnalysisPeriod).Last().Sma.Value;
                if (Min.HasValue && val < Min.Value)
                {
                    result = BuySell.Buy;
                    status = RangeStatus.BelowMin;
                    value = val;
                    //Log($"Ema: {ema.Ema} Status: {status} Result: {result}", LogLevel.Debug, t);
                }
                else if (Max.HasValue && val > Max.Value)
                {
                    result = BuySell.Sell;
                    status = RangeStatus.AboveHigh;
                    value = val;

                    //Log($"Ema: {ema.Ema} Status: {status} Result: {result}", LogLevel.Debug, t);
                }

            }

            return new RangeSignalResult(this, status, t ?? DateTime.Now) { Value = value, finalResult = result };
        }

    }


}
