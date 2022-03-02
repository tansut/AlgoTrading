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
                if (this.Average == Average.Ema)
                {
                    return List.List.GetEma(List.Count, Candle).Last().Ema.Value;
                }
                else
                {
                    return List.List.GetSma(List.Count, Candle).Last().Sma.Value;
                }
            }
        }


        public int Count => List.Count;
    }

    public class AnalyserBase : Signal
    {
        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }

        public int InitialAnalyseSize { get; set; }
        public int InitialCollectSize { get; set; }


        public Average CollectAverage { get; set; }
        public Average AnalyseAverage { get; set; }

        public AnalyseList CollectList { get; set; }
        public AnalyseList AnalyseList { get; set; }

        public decimal SignalSensitivity { get; set; } = 1.0M;

        public AnalyserBase(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {

        }



        protected virtual void AdjustSensitivityInternal(double ratio, string reason)
        {
            AnalyseSize = InitialAnalyseSize + Convert.ToInt32((InitialAnalyseSize * (decimal)ratio));
            
            CollectSize = InitialCollectSize + Convert.ToInt32((InitialCollectSize * (decimal)ratio));
            CollectList.Resize(CollectSize);
            AnalyseList.Resize(AnalyseSize);
            Monitor("sensitivity/collectsize", (decimal)CollectSize);
            Monitor("sensitivity/analysesize", (decimal)AnalyseSize);
            //Monitor("sensitivity/ratio", (decimal)ratio);
            //Log($"{reason}: Adjusted to (%{((decimal)ratio * 100).ToCurrency()}): c:{CollectSize} a:{AnalyseSize}", LogLevel.Debug);
        }


        public override void Init()
        {
            CollectSize = Convert.ToInt32(CollectSize * SignalSensitivity);
            AnalyseSize = Convert.ToInt32(AnalyseSize * SignalSensitivity);
            InitialAnalyseSize = AnalyseSize;
            InitialCollectSize = CollectSize;
            CollectList = new AnalyseList(CollectSize, CollectAverage);
            AnalyseList = new AnalyseList(AnalyseSize, AnalyseAverage);
            ResetInternal();
            MonitorInit("sensitivity/collectsize", (decimal)CollectSize);
            MonitorInit("sensitivity/analysesize", (decimal)AnalyseSize);
            //MonitorInit("sensitivity/ratio", 0);
            base.Init();
        }

        protected override void ResetInternal()
        {

            CollectList.Resize(CollectSize);
            AnalyseList.Resize(AnalyseSize);
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
