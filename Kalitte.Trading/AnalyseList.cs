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
        public MyQuote Current { get; set; }
        

        public OHLCType Candle { get; private set; }

        public AnalyseList(int size, Average average, OHLCType candle = OHLCType.Close)
        {
            this.Average = average;
            this.List = new FinanceList<MyQuote>(size);
            this.SpeedHistory = new FinanceList<MyQuote>(60 * (int)SpeedMinutes + 1);
            this.Candle = candle;
            
        }

        public void SetPeriods(DateTime date, decimal? value = null)
        {
            if (Period != BarPeriod.Sec)
            {
                Helper.SymbolSeconds(this.Period.ToString(), out int seconds);
                var t1 = Helper.RoundDown(date, TimeSpan.FromSeconds(seconds));
                var t2 = Helper.RoundUp(date, TimeSpan.FromSeconds(seconds));
                if (Current == null && value.HasValue)
                {
                    Current = MyQuote.Create(t1, value.Value, Candle);
                }
                else if (Current != null)
                {
                    if (Current.Date == t1)
                    {
                        if (value.HasValue) Current.Update(value.Value, Candle);
                    }
                    else if (Current.Date < t1)
                    {
                        this.List.Push(Current);
                        if (value.HasValue) Current = MyQuote.Create(t1, value.Value, Candle);
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
            lookback = lookback == 0 ? count : Math.Min(lookback, count);
            List<MyQuote> resultList = null;

            //if (lookback == 0) return new List<MyQuote> { Current };
            //else if (lookback == count) lookback = 5;
            if (this.Average == Average.Ema)
            {
                var result = list.GetEma(lookback, (CandlePart)ohlc);
                resultList = result.Select(p => new MyQuote() { Date = p.Date, Close = p.Ema.HasValue ? (decimal)p.Ema.Value:0 }).ToList();
            } else
            {
                var result = list.GetSma(lookback, (CandlePart)ohlc);
                resultList = result.Select(p => new MyQuote() { Date = p.Date, Close = p.Sma.HasValue ? (decimal)p.Sma.Value:0 }).ToList();
            }

            if (count <= lookback && wamupList != null)
            {
                wamupList.Add(resultList.Last());
                return wamupList;
            }
            else return resultList;
        }


        public decimal LastValue(int lookback = 0, OHLCType? ohlc = null)
        {
            return Count == 0 ? Current.Get(Candle): Averages(lookback, ohlc).Last().Close;
            //ohlc = ohlc ?? Candle;
            //var list = this.List.List;
            //var count = list.Count;
            //lookback = lookback == 0 ? count : Math.Min(lookback, count);
            //if (this.Average == Average.Ema)
            //{
            //    return list.GetEma(lookback, (CandlePart)ohlc).Last().Ema.Value;
            //}
            //else
            //{
            //    return list.GetSma(lookback, (CandlePart)ohlc).Last().Sma.Value;
            //}
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

        internal void ResetWarmup()
        {
            WarmupList.Clear();
        }
    }

}
