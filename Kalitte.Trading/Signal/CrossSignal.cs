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
        private FinanceBars differenceBars;
        private FinanceBars emaBars;


        private decimal lastEma = 0;
        private decimal lastCross = 0;




        public CrossSignal(string name, string symbol, Kalitte.Trading.Matrix.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        public override void Start()
        {
            base.Start();            
        }

        protected override void ResetInternal()
        {

        }

        public override void Stop()
        {
            base.Stop();
              
        }

        public override void Init()
        {
            differenceBars = new FinanceBars(Periods);
            emaBars = new FinanceBars(Periods);
            lastEma = 0;
            lastCross = 0;
        }



        protected override void Colllect()
        {

        }

        public override string ToString()
        {
            return $"{base.ToString()}: {i1k.ToString()}/{i2k.ToString()}] period: {Periods} avgChange: {AvgChange}";
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
            decimal i1Val = i1.CurrentValue, i2Val = i2.CurrentValue;

            var l1 = i1k.NextValue(mp);
            var l2 = i2k.NextValue(mp);

            var newResultBar = new Quote() { Date = t ?? DateTime.Now, Close = l1 - l2 };
            differenceBars.Push(newResultBar);



            var ldif = Math.Round(l1 - l2, 5);
            var idif = Math.Round(i1Val - i2Val, 5);

            if (Math.Abs(ldif - idif) > 0.2M && !Simulation)
            {
                Log("-- Too much indicator difference between us and matrix --", LogLevel.Debug, t);
                Log($"Bardata: {i1k.InputBars.Last}", LogLevel.Debug, t);
                Log($"Currents: my1: {l1}  i1: {i1Val} my2: {l2} l2: {i2Val}", LogLevel.Debug, t);
                Log($"Difs: mp:{mp}  ldif: {ldif} idif: {idif} diff: {ldif - idif}", LogLevel.Debug, t);
            }

            if (differenceBars.Count >= Periods)
            {
                var cross = differenceBars.Cross(0);
                var ema = differenceBars.List.GetEma(Periods).Last();
                //emaBars.Push(new MyQuote() { Date = ema.Date, Close = ema.Ema.Value });

                //if (emaBars.Count >= Periods)
                //{
                //    var fl = emaBars.FirstLast;

                //    var speed = (fl.Item2.Close - fl.Item1.Close) / emaBars.Count;

                //    //if (t.Value.Second % 15 == 0) Log($"Speed of MA difs is {speed}", LogLevel.Debug, t);
                //}


                if (lastCross == 0 && cross != 0)
                {
                    lastCross = cross;
                    differenceBars.Clear();
                }

                if (lastCross > 0 && ema.Ema.Value > AvgChange) finalResult = OrderSide.Buy;
                else if (lastCross < 0 && ema.Ema.Value < -AvgChange) finalResult = OrderSide.Sell;

                //lastCross = finalResult.HasValue ? 0 : lastCross;

                //if (lastEma < 0 && ema.Ema.Value > AvgChange) finalResult = OrderSide.Buy;
                //else if (lastEma > 0 && ema.Ema.Value < -AvgChange) finalResult = OrderSide.Sell;

                if (finalResult.HasValue)
                {                    
                    Log($"Status: order:{finalResult}, mp:{mp}, lastCross:{lastCross}, cross:{cross}, lastEma:{lastEma}, ema:{ema.Ema}, split:{AvgChange}", LogLevel.Info, t);
                    lastCross = 0;                    
                }

                if (lastEma == 0) lastEma = ema.Ema.Value;

                lastEma = finalResult.HasValue ? 0 : lastEma;
            }

            return new SignalResultX(this)
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