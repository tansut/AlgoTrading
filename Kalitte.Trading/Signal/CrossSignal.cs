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
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using Matriks.Indicators;
using Matriks.Lean.Algotrader.Models;

namespace Kalitte.Trading
{

    public class CrossSignal : Signal
    {
        public IIndicator i1 = null;
        public IIndicator i2 = null;


        public ITradingIndicator i1k;
        public ITradingIndicator i2k;

        public decimal AvgChange = 0.3M;
        public int Periods = 5;
        public int PriceCollectionPeriod = 5;
        private FinanceBars differenceBars;
        private FinanceBars priceBars;


        private decimal lastCross = 0;
        public int NextOrderMultiplier = 1;
        public bool UseSma = true;


        public CrossSignal(string name, string symbol, Kalitte.Trading.Matrix.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }



        protected override void ResetInternal()
        {
            priceBars.Clear();
            differenceBars.Clear();
            lastCross = 0;
        }



        public override void Init()
        {
            differenceBars = new FinanceBars(Periods);
            priceBars = new FinanceBars(PriceCollectionPeriod);
            ResetInternal();
        }



        protected override void Colllect()
        {

        }

        public override string ToString()
        {
            return $"{base.ToString()}: {i1k.ToString()}/{i2k.ToString()}] period: {Periods} pricePeriod: {PriceCollectionPeriod} useSma: {UseSma} avgChange: {AvgChange}";
        }



        protected SignalResultX CalculateSignal(DateTime? t = null)
        {
            OrderSide? finalResult = null;
            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp == 0)
            {
                mp = i1k.InputBars.Last.Close;
                Log($"Used last close bar price { mp } since market price is unavailable.", LogLevel.Warning, t);
            }


            priceBars.Push(new Quote() { Date = t ?? DateTime.Now, Close = mp });

            if (priceBars.IsFull && mp >= 0)
            {

                decimal mpAverage = priceBars.List.GetSma(priceBars.Count).Last().Sma.Value;
                priceBars.Clear();

                var l1 = i1k.NextValue(mpAverage);
                var l2 = i2k.NextValue(mpAverage);

                var newResultBar = new Quote() { Date = t ?? DateTime.Now, Close = l1 - l2 };
                differenceBars.Push(newResultBar);

                var ldif = Math.Round(l1 - l2, 5);

                var cross = differenceBars.Cross(0);

                if (lastCross == 0 && cross != 0)
                {
                    lastCross = cross;
                    differenceBars.Clear();
                }

                if (differenceBars.Count >= Periods)
                {

                    var lastAvg = UseSma ? differenceBars.List.GetSma(Periods).Last().Sma.Value : differenceBars.List.GetEma(Periods).Last().Ema.Value;
                    
                    decimal last1 = i1k.Results.Last().Value;
                    decimal last2 = i2k.Results.Last().Value;

                    if (lastCross != 0 && lastAvg > AvgChange) finalResult = OrderSide.Buy;
                    else if (lastCross != 0 && lastAvg < -AvgChange) finalResult = OrderSide.Sell;


                    Log($"Status: order:{finalResult}, lastAvg: {lastAvg} i1Last: {last1} i2Last:{last2} mpNow:{mp}, mpAvg: {mpAverage}, lastCross:{lastCross}, cross:{cross}", LogLevel.Debug, t);

                    if (finalResult.HasValue)
                    {

                        differenceBars.Clear();
                    }


                }
            }




            return new SignalResultX(this, t ?? DateTime.Now)
            {
                finalResult = finalResult
            };

            //if (lastCross == 0 && cross !=  0) lastCrossValue = cross;

            //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
            //if (lastCross > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
            //else if (lastCross < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

            //lastCross = finalResult.HasValue ? 0 : lastCross;



            //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);
            //var changedDirection = lastEma * 







        }



        //protected SignalResultX CalculateSignal(DateTime? t = null)
        //{

        //    OrderSide? finalResult = null;

        //    var mp = Algo.GetMarketPrice(Symbol, t);

        //    if (mp == 0 && useLastPriceIfMissing)
        //    {
        //        Log($"No price was found, used last price {lastMarketPrice}", LogLevel.Warning, t);
        //        mp = lastMarketPrice;
        //    }
        //    else lastMarketPrice = mp;

        //    if (mp > 0)
        //    {
        //        var val =  useMyIndicators ?
        //            bars.Count == 0 ?  i1k.Values.Last() - i2k.Values.Last() :
        //            i1k.LastValue(mp) - i2k.LastValue(mp) 
        //            : i1.CurrentValue - i2.CurrentValue;
        //        //var val = useMyIndicators ? i1k.Values.Last() - i2k.Values.Last() : i1.CurrentValue - i2.CurrentValue;

        //        bars.Push(new Quote(val));
        //        if (useMyIndicators)
        //        {
        //            Log($"i1k: {i1k.LastValue(mp)} i1:{i1.CurrentValue} mp: {mp}", LogLevel.Debug, t);
        //            Log($"i2k: {i2k.LastValue(mp)} i2:{i2.CurrentValue} mp: {mp}", LogLevel.Debug, t);
        //        }

        //        var cross = bars.Cross(0);
        //        var ema = bars.Ema().Last();


        //        if (lastEma < 0 && ema > AvgChange) finalResult = OrderSide.Buy;
        //        else if (lastEma > 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

        //        if (lastEma == 0) lastEma = ema;

        //        lastEma = finalResult.HasValue ? 0 : lastEma;

        //        Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);

        //        //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
        //    }
        //    else Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} no market price for {t}", LogLevel.Debug, t);




        //    //if (lastCrossValue == 0 && cross !=  0) lastCrossValue = cross;

        //    //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
        //    //if (lastCrossValue > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
        //    //else if (lastCrossValue < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

        //    //lastCrossValue = finalResult.HasValue ? 0 : lastCrossValue;



        //    //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);
        //    //var changedDirection = lastEma * 




        //    return new SignalResultX(this)
        //    {
        //        finalResult = finalResult
        //    };


        //}

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var current = CalculateSignal(t);
            return current;
        }
    }
}