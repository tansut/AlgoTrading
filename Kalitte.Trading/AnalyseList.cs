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

        public Average Average { get; set; }
        public FinanceList<MyQuote> List { get; private set; }
        public AnalyseList Speed { get; set; }
        public FinanceList<MyQuote> SpeedHistory { get; private set; }
        //public FinanceList<MyQuote> SpeedValues { get; private set; }

        public CandlePart Candle { get; private set; }

        public AnalyseList(int size, Average average, CandlePart candle = CandlePart.Close)
        {
            this.Average = average;
            this.List = new FinanceList<MyQuote>(size);
            this.SpeedHistory = new FinanceList<MyQuote>(60 * (int)SpeedMinutes + 1);
            this.Candle = candle;
        }



        public AnalyseList Collect(decimal val, DateTime t)
        {
            return this.Collect(t, val);
        }

        public bool SpeedInitialized
        {
            get
            {
                return this.Speed != null;
            }
        }

        public AnalyseList Collect(DateTime date, decimal value)
        {
            var q = new MyQuote() { Date = date };
            q.Set(value, Candle);
            this.List.Push(q);
            return this;
        }

        public void Clear()
        {
            List.Clear();
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

        public decimal LastValue
        {
            get
            {
                var list = this.List.List;
                var count = list.Count;
                if (this.Average == Average.Ema)
                {
                    return list.GetEma(count, Candle).Last().Ema.Value;
                }
                else
                {
                    return list.GetSma(count, Candle).Last().Sma.Value;
                }
            }
        }

        public int Count => List.Count;

        internal void ResetSpeed(decimal value, DateTime t)
        {
            Speed = new AnalyseList(this.List.QueSize, Average.Sma);
            Speed.Collect(value, t);
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
            var dif = (prevBar == null ? Speed.List.First.Close : prevBar.Close) - LastValue;
            decimal value = Math.Abs(dif) / SpeedMinutes;
            Speed.Collect(time, value);
            return Speed.LastValue;
        }
    }

}
