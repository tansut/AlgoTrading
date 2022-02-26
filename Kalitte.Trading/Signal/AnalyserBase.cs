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
using Kalitte.Trading.Algos;
using Skender.Stock.Indicators;

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
        //public CandlePart Candle { get; private set; }

        public AnalyseList(int size, Average average, CandlePart candle = CandlePart.Close)
        {
            this.Size = size;
            this.Average = average;
            this.List = new FinanceList<IQuote>(Size);
            //this.Candle = candle;
        }

        public void Collect(decimal val)
        {
            this.Collect(DateTime.Now, val);
        }

        public void Collect(DateTime date, decimal value)
        {
            var q = new MyQuote() { Date = date, Close=value };            
            this.List.Push(q);
        }

        public void Clear()
        {
            List.Clear();
        }

        public virtual bool Ready()
        {
            return List.IsFull;
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
                if (this.Average == Average.Ema)
                {
                    return List.List.GetEma(List.Count).Last().Ema.Value;
                } else
                {
                    return List.List.GetSma(List.Count).Last().Sma.Value;
                }                
            }
        }


        public int Count => List.Count;
    }

    public class AnalyserBase : Signal
    {
        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }
        public Average CollectAverage { get; set; }
        public Average AnalyseAverage { get; set; }


        public AnalyseList CollectList { get; set; }
        public AnalyseList AnalyseList { get; set; }

        public AnalyserBase(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {
            
        }

        public override void Init()
        {
            CollectList = new AnalyseList(CollectSize, CollectAverage);
            AnalyseList = new AnalyseList(AnalyseSize, AnalyseAverage);
            ResetInternal();
            base.Init();
        }

        protected override void ResetInternal()
        {
            CollectList.Clear();
            AnalyseList.Clear();
            base.ResetInternal();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            return null;
        }



    }


}
