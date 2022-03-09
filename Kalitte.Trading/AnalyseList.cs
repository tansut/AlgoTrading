﻿using Skender.Stock.Indicators;
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
        public int Size { get; set; }
        public Average Average { get; set; }
        public FinanceList<IQuote> List { get; private set; }
        public CandlePart Candle { get; private set; }

        public AnalyseList(int size, Average average, CandlePart candle = CandlePart.Close)
        {
            this.Size = size;
            this.Average = average;
            this.List = new FinanceList<IQuote>(Size);
            this.Candle = candle;
        }

        public void Collect(decimal val)
        {
            this.Collect(DateTime.Now, val);
        }

        public void Collect(DateTime date, decimal value)
        {
            var q = new MyQuote() { Date = date };
            q.Set(value, Candle);
            this.List.Push(q);
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
            this.Size = newSize;
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
    }

}
