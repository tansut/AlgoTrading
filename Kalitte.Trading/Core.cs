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

namespace Kalitte.Trading
{
    public enum OHLC
    {
        Open = 0,
        High = 1,
        Low = 2,
        Close = 3,
        Volume = 4,
        WClose = 5,
        Diff = 6,
        DiffPercent = 7,
        Undivided = 8,
        Other = 9
    }

    public static class Extensions
    {
        public static decimal ToCurrency(this decimal d)
        {
            return Math.Round(d, 2, MidpointRounding.AwayFromZero);
        }
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 4
    }

    public interface SignalManager
    {
        void Log(string text, LogLevel level = LogLevel.Info);
        bool CrossAboveX(IIndicator i1, IIndicator i2, DateTime t);
        bool CrossBelowX(IIndicator i1, IIndicator i2, DateTime t);
    }

    public interface ICrossCalculator
    {
        bool CrossAboveX(TopQue q1, TopQue q2, decimal dif);
    }

                    //    if (avgEmaDif > AvgChange) finalResult = OrderSide.Buy;
                    //else if (avgEmaDif< -AvgChange) finalResult = OrderSide.Sell;

    public class CrossCalculator : ICrossCalculator
    {
        public bool CrossAboveX(TopQue q1, TopQue q2, decimal dif)
        {
            var c1 = q1.First() < q2.First() && q1.Last() > q2.Last();

            return c1;



        }

        public bool CrossBelowX(TopQue q1, TopQue q2, decimal dif)
        {
            var c1 = q1.First() > q2.First() && q1.Last() < q2.Last();

            return c1;
        }
    }

    public class TopQue: List<decimal>
    {
        private int Period = 5;
        private decimal alpha = 0.2M;

        //public  List<decimal> CalcEma(int lookbackPeriods)
        //{
        //    // check parameter arguments


        //    // initialize
        //    int length = bdList.Count;
        //    List<decimal> results = new List<decimal>(length);

        //    double k = 2d / (lookbackPeriods + 1);
        //    double? lastEma = 0;
        //    int initPeriods = Math.Min(lookbackPeriods, length);

        //    for (int i = 0; i < initPeriods; i++)
        //    {
        //        lastEma += bdList[i].Value;
        //    }

        //    lastEma /= lookbackPeriods;

        //    // roll through quotes
        //    for (int i = 0; i < length; i++)
        //    {
        //        BasicD h = bdList[i];
        //        int index = i + 1;

        //        EmaResult result = new()
        //        {
        //            Date = h.Date
        //        };

        //        if (index > lookbackPeriods)
        //        {
        //            double? ema = lastEma + (k * (h.Value - lastEma));
        //            result.Ema = (decimal?)ema;
        //            lastEma = ema;
        //        }
        //        else if (index == lookbackPeriods)
        //        {
        //            result.Ema = (decimal?)lastEma;
        //        }

        //        results.Add(result);
        //    }

        //    return results;
        //}


        public void Push(decimal quote)
        {
            Add(quote);
            if (Count == Period + 1)
                RemoveAt(0);
            
        }

        public TopQue(int period)
        {
            Period = period;
            
            alpha = 2M / (period + 1);
        }

        //public void Clear()
        //{
        //    Quotes.Clear();
        //}

        //public int Count { get
        //    {
        //        return Quotes.Count;
        //    }
        //}

        //public decimal Average { get { if (Count == 0) return 0; return Average(); } }

        public List<decimal> CalcEma(int lookbackPeriods) 
        {
            List<decimal> emaArray = new List<decimal>();
            double k = 2D / (lookbackPeriods + 1);
            int initPeriods = Math.Min(lookbackPeriods, this.Count);
            double lastEma = 0;

            for (int i = 0; i < initPeriods; i++)
            {
                lastEma += (double)this[i];
            }

            lastEma /= lookbackPeriods;

            decimal result = 0;

            for (var i = 0; i < this.Count; i++)
            {
                int index = i + 1;

                if (index > lookbackPeriods)
                {
                    double ema = lastEma + (k * ((double)this[i] - lastEma));
                    result = (decimal)ema;
                    lastEma = ema;
                }
                else if (index == lookbackPeriods)
                {
                    result = (decimal)lastEma;
                }

                emaArray.Add(result);
            }

            return emaArray;
        }

        public decimal ExponentialMovingAverage
        {

            get
            {
                try
                {
                    return this.CalcEma((this.Count)).Last();
                } catch(Exception ex)
                {
                    throw new Exception("Error in list:" + string.Join(",", this.Select(p => p.ToString())), ex);
                }
                
                //return this.DefaultIfEmpty()
                //             .Aggregate((ema, nextQuote) => alpha * nextQuote + (1 - alpha) * ema);
            }
        }
    }
}
