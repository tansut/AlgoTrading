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
    public enum TrendDirection
    {
        ReturnUp,
        ReturnDown,
        MoreUp,
        LessUp,
        MoreDown,
        LessDown,
        None
    }

    public enum TrendReference
    {
        LastBar,
        LastCheck
    }

    public enum ResetList
    {
        None,
        AfterTrend,
        Always
    }

    public class TrendResult
    {
        public TrendDirection Direction { get; set; }
        public decimal OldValue { get; set; }
        public decimal NewValue { get; set; }
        public TrendResult Reference { get; set; }
        public DateTime Date { get; set; }

        public decimal Change
        {
            get
            {
                return NewValue - OldValue;
            }
        }

        public decimal? SpeedPerSecond
        {
            get
            {
                if (Reference == null) return null;
                var seconds = (this.Date - this.Reference.Date).TotalSeconds;
                if (seconds == 0) return null;
                return (Math.Abs(this.NewValue - this.Reference.NewValue) / (decimal)seconds);
            }
        }

        public TrendResult()
        {
            Direction = TrendDirection.None;
        }

        public override string ToString()
        {
            if (Reference == null)
            {
                return $"date: {Date}, dir: {Direction} ch: {Change} nv:{NewValue} ov: {OldValue}";
            }
            else
                return $"date: {Date}, dir: {Direction} ch: {Change} nv:{NewValue} ov: {OldValue} ref:[{Reference.Date}, {Reference.Direction}, {Reference.Change}]";

        }

        public override int GetHashCode()
        {
            return Direction.GetHashCode();
        }
    }

    public class TrendSignalResult : SignalResult
    {
        public TrendResult Trend { get; set; }
        public IQuote[] CollectList { get; set; }
        public IQuote[] AnalyseList { get; set; }
        public decimal MarketPrice { get; set; }


        public TrendSignalResult(Signal signal, DateTime t) : base(signal, t)
        {
        }

        public override string ToString()
        {
            IQuote cl = CollectList == null ? null:  CollectList.LastOrDefault();
            IQuote al = AnalyseList == null ? null: AnalyseList.LastOrDefault();
            return $"{base.ToString()} mp: {this.MarketPrice} cl:{(cl == null ? 0:cl.Close)} al:{(al == null ? 0 : al.Close)} | {Trend}";
        }

        public override int GetHashCode()
        {
            return Trend.GetHashCode();
        }
    }

    public class TrendSignal : AnalyserBase
    {
        public ITechnicalIndicator i1k;
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        private List<TrendResult> BarTrendResults;
        public TrendReference ReferenceType { get; set; } = TrendReference.LastBar;
        private decimal? lastValue = null;
        public bool UseSma = true;
        public ResetList HowToReset { get; set; } = ResetList.None;


        public TrendSignal(string name, string symbol, AlgoBase owner, decimal? min = null, decimal? max  = null) : base(name, symbol, owner)
        {
            Min = min;
            Max = max;
        }

        public override void Init()
        {
            BarTrendResults = new List<TrendResult>();
            Indicators.Add(i1k);
            i1k.InputBars.ListEvent += base.InputbarsChanged;
            generateDerivs();
            MonitorInit("value", 0);
            MonitorInit("speed", 0);
            base.Init();
        }

        protected override void ResetInternal()
        {
            base.ResetInternal();
            generateDerivs();            
        }


        protected override void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {
            generateDerivs();
        }



        public override string ToString()
        {
            return $"{base.ToString()}: Range: {Min}-{Max} Period: {AnalyseSize} PriceCollection: {CollectSize} useSma: {UseSma}";
        }


        private TrendResult getTrendDirection(decimal oldVal, decimal newVal, TrendResult reference = null)
        {
            var change = newVal - oldVal;
            var result = new TrendResult();
            result.OldValue = oldVal;
            result.NewValue = newVal;
            if (reference == null)
            {
                if (change > 0) result.Direction = TrendDirection.ReturnUp;
                else if (change < 0) result.Direction = TrendDirection.ReturnDown;
            }
            else
            {
                var delta =  reference.Change;
                if (change < 0 && delta < 0)
                {
                    if (delta < change) result.Direction = TrendDirection.LessDown;
                    else if (delta > change) result.Direction = TrendDirection.MoreDown;
                }
                else if (change > 0 && delta > 0)
                {
                    if (delta < change) result.Direction = TrendDirection.MoreUp;
                    else if (delta > change) result.Direction = TrendDirection.LessUp;
                }
                else if (change < 0)
                {
                    result.Direction = TrendDirection.ReturnDown;
                }
                else if (change > 0)
                {
                    result.Direction = TrendDirection.ReturnUp;
                }
                result.Reference = reference;
            }


            return result;
        }

        private void generateDerivs()
        {
            BarTrendResults.Clear();
            var list = i1k.Results;
            for (var i = 0; i < list.Count; i++)
            {
                var index = i + 1;
                if (index >= list.Count) break;
                if (!list[index].Value.HasValue || !list[i].Value.HasValue) continue;

                var tr = getTrendDirection(list[i].Value.Value, list[index].Value.Value, BarTrendResults.LastOrDefault());
                tr.Date = list[index].Date;
                BarTrendResults.Add(tr);
            }
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var result = new TrendSignalResult(this, t ?? DateTime.Now);
            result.Trend = new TrendResult();

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            if (CollectList.Ready && mp >= 0)
            {
                decimal mpAverage = CollectList.LastValue; 

                var l1 = i1k.NextValue(mpAverage).Value.Value;

                AnalyseList.Collect(l1);

                if (AnalyseList.Ready && BarTrendResults.Count > 0)
                {
                    var currentVal = AnalyseList.LastValue; 
                    var lastReference = i1k.Results.Last().Value.Value;

                    if (this.ReferenceType == TrendReference.LastCheck && lastValue.HasValue)
                    {
                        lastReference = lastValue.Value;
                    }
                    result.MarketPrice = mp;
                    result.CollectList = CollectList.List.ToArray;
                    result.AnalyseList = AnalyseList.List.ToArray;

                    result.Trend =  getTrendDirection(lastReference, currentVal, this.ReferenceType == TrendReference.LastCheck ? null:  BarTrendResults.LastOrDefault());
                    result.Trend.Date = t ?? DateTime.Now;

                    Watch("value", result.Trend.NewValue);
                    var speed = result.Trend.SpeedPerSecond;
                    if (speed.HasValue) Watch("speed", speed.Value);

                    var limit = currentVal;
                    var checkedLimits = !Min.HasValue && !Max.HasValue;

                    if (Min.HasValue && limit <= Min.Value)
                    {
                        checkedLimits = true;
                    }
                    else if (Max.HasValue && limit >= Max.Value)
                    {
                        checkedLimits = true;
                    }

                    if (!checkedLimits)
                    {
                        result.Trend.Direction = TrendDirection.None;
                    }

                    if (result.Trend.Direction != TrendDirection.None)
                    {
                        result.finalResult = BuySell.Sell;
                        if (HowToReset == ResetList.AfterTrend || HowToReset == ResetList.Always) AnalyseList.Clear();
                    } else
                    {
                        if (HowToReset == ResetList.Always) AnalyseList.Clear();
                    }

                    lastValue = currentVal;

                    Log($"trend-signal: result: {result}", LogLevel.Verbose, t);
                }
            }

            return result;

        }


    }
}
