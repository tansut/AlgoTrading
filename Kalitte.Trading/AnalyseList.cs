// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public enum Average
    {
        Ema = 0,
        Sma = 1
    }

    public class AnalyseList
    {
        public decimal SpeedMinutes { get; set; } = 1M;
        public int SpeedAnalyse { get; set; } = 0;
        public bool AbsoluteSpeed { get; set; } = true;
        public Average Average { get; set; }
        public FinanceList<MyQuote> List { get; private set; }
        public AnalyseList Speed { get; set; }
        public FinanceList<MyQuote> SpeedHistory { get; private set; }
        public BarPeriod Period { get; set; } = BarPeriod.Sec;
        public List<MyQuote> WarmupList { get; set; }

        public OHLCType Candle { get; private set; }

        public AnalyseList(int size, Average average, OHLCType candle = OHLCType.Close)
        {
            this.Average = average;
            this.List = new FinanceList<MyQuote>(size);
            this.SpeedHistory = new FinanceList<MyQuote>(60 * (int)SpeedMinutes + 1);
            this.Candle = candle;
            WarmupList = new List<MyQuote>();

        }

        public void SetPeriods(DateTime date, decimal? value = null)
        {
            if (Period != BarPeriod.Sec)
            {
                Helper.SymbolSeconds(this.Period.ToString(), out int seconds);
                var t1 = Helper.RoundDown(date, TimeSpan.FromSeconds(seconds));
                var t2 = Helper.RoundUp(date, TimeSpan.FromSeconds(seconds));
                var lastItem = List.Last;
                if (lastItem == null && value.HasValue)
                {
                    lastItem = MyQuote.Create(t1, value.Value, Candle);
                    List.Push(lastItem);
                }
                else if (lastItem != null)
                {
                    if (lastItem.Date == t1)
                    {
                        if (value.HasValue) lastItem.Update(value.Value, Candle);
                    }
                    else if (lastItem.Date < t1)
                    {
                        if (value.HasValue) List.Push(MyQuote.Create(t1, value.Value, Candle));
                    }
                }
            }

        }


        public AnalyseList Collect(decimal value, DateTime date)
        {
            if (Period == BarPeriod.Sec)
            {
                var q = MyQuote.Create(date, value, Candle);
                this.List.Push(q);
            }
            else
            {
                SetPeriods(date, value);
            }
            return this;
        }

        public bool SpeedInitialized
        {
            get
            {
                return this.Speed != null;
            }
        }



        public void Clear()
        {
            List.Clear();
            WarmupList.Clear();
            if (Speed != null) Speed.Clear();
        }

        public virtual bool Ready
        {
            get
            {
                return List.IsFull;
            }

        }

        public void Resize(int newSize)
        {
            List.Resize(newSize);
            WarmupList.Clear();
        }

        public decimal Rsi(int lookback = 0)
        {
            var list = this.List.List;
            var count = list.Count;
            lookback = lookback == 0 ? count - 1 : Math.Min(lookback, list.Count - 1);
            return lookback <= 1 ? 0 : (decimal)list.GetRsi(lookback).Last().Rsi.Value;
        }

        public IList<MyQuote> Averages(int lookback = 0, OHLCType? ohlc = null, IList<MyQuote> wamupList = null)
        {
            ohlc = ohlc ?? Candle;
            var list = this.List.List;
            var count = list.Count;
            //lookback = lookback == 0 ? Math.Max(1, count / 2) : lookback;
            lookback = BestLookback(count, lookback);
            List<MyQuote> resultList = null;

            if (this.Average == Average.Ema)
            {
                var result = list.GetEma(lookback, (CandlePart)ohlc);
                resultList = result.Select(p => new MyQuote() { Date = p.Date, Close = p.Ema.HasValue ? (decimal)p.Ema.Value : 0 }).ToList();
            }
            else
            {
                var result = list.GetSma(lookback, (CandlePart)ohlc);
                resultList = result.Select(p => new MyQuote() { Date = p.Date, Close = p.Sma.HasValue ? (decimal)p.Sma.Value : 0 }).ToList();
            }
            wamupList = wamupList ?? this.WarmupList;
            if (count <= lookback && wamupList != null)
            {
                var last = resultList.Last();
                var sum = wamupList.Sum(p => p.Close) + last.Close;
                var avg = sum / (wamupList.Count + 1);
                wamupList.Add(MyQuote.Create(last.Date, avg, Candle));
                return wamupList;
            }
            else
                return resultList;
        }

        public int BestLookback(int listSize, int lookback)
        {
            if (lookback == 0) lookback = Math.Max(listSize / 2, 1);
            var dif = lookback - listSize;
            if (dif < 0) return lookback;
            return Math.Max(1, listSize - 1);
        }

        internal void Init(IEnumerable<MyQuote> list)
        {
            List.Clear();
            foreach (var item in list)
            {
                List.Push(MyQuote.Create(item));
            }

        }

        public decimal LastValue(int lookback = 0, OHLCType? ohlc = null, List<MyQuote> warmupList = null)
        {
            return Averages(lookback, ohlc, warmupList).Last().Close;
        }

        public int Count => List.Count;

        internal void ResetSpeed(decimal value, DateTime t)
        {
            Speed = new AnalyseList(this.List.QueSize, Average.Sma);
            Speed.AbsoluteSpeed = false;
            SpeedHistory.Clear();
            SpeedHistory.Push(new MyQuote() { Date = t, Close = value });
        }

        internal void UpdateSpeed(DateTime time, decimal value)
        {
            SpeedHistory.Push(new MyQuote() { Date = time, Close = value });
        }

        internal decimal CalculateSpeed(DateTime time)
        {
            var fDate = time.AddMinutes(-(double)SpeedMinutes);
            var prevBar = SpeedHistory.List.FirstOrDefault(p => p.Date == fDate);
            var dif = (prevBar == null ? SpeedHistory.First.Close : prevBar.Close) - LastValue();
            var minutes = prevBar == null && SpeedHistory.Count > 0 ? SpeedHistory.Count / 60M : SpeedMinutes;
            //decimal value = dif / minutes;
            decimal value = (AbsoluteSpeed ? Math.Abs(dif) : dif) / minutes;
            Speed.Collect(value, time);
            return Speed.LastValue();
            //return value;
        }


    }

}
