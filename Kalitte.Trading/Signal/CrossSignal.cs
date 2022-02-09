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

    public class CrossSignal : Signal
    {
        public IIndicator i1 = null;
        public IIndicator i2 = null;


        public Kalitte.Trading.Indicators.IndicatorBase i1k;
        public Kalitte.Trading.Indicators.IndicatorBase i2k;

        public decimal AvgChange = 0.3M;
        public int Periods = 5;
        private Bars differenceBars;
        private Bars priceBars;

        bool useMyIndicators = false;
        bool useLastPriceIfMissing = true;
        decimal lastMarketPrice = 0;

        private decimal lastEma = 0;
        private decimal lastCross = 0;



        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        public override void Start()
        {
            differenceBars = new Bars(Periods);
            priceBars = new Bars(2);
            base.Start();
            Log($"Started with {i1.GetType().Name}[{i1.Period}]/{i2.GetType().Name}[{i2.Period}] period: {Periods} avgChange: {AvgChange}", LogLevel.Info);
        }

        public override void Stop()
        {
            base.Stop();
        }


        protected override void Colllect()
        {

        }

        protected SignalResultX CalculateSignal(DateTime? t = null)
        {
            OrderSide? finalResult = null;
            var mp = Algo.GetMarketPrice(Symbol, t); 

            if (mp == 0)
            {
                mp = i1k.InputBars.Latest.Close;
                Log($"Used last close bar price { mp } since market price is unavailable.", LogLevel.Warning, t);
            }
            decimal i1Val = i1.CurrentValue, i2Val = i2.CurrentValue;

            var l1 = i1k.NextValue(mp);
            var l2 = i2k.NextValue(mp);            

            var newResultBar = new Quote(t ?? DateTime.Now, l1 - l2);
            differenceBars.Push(newResultBar);

            var ldif = Math.Round(l1 - l2, 5);
            var idif = Math.Round(i1Val - i2Val, 5);

            if (Math.Abs(ldif - idif) > 0.2M && !Simulation)
            {
                Log("-- Too much indicator difference between us and matrix --", LogLevel.Debug, t);
                Log($"Currents: my1: {l1}  i1: {i1Val} my2: {l2} l2: {i2Val}", LogLevel.Debug, t);
                Log($"Difs: mp:{mp}  ldif: {ldif} idif: {idif} diff: {ldif - idif}", LogLevel.Debug, t);
            }
            
            if (differenceBars.Count >= Periods)
            {                
                var cross = differenceBars.Cross(0);
                var ema = differenceBars.Ema(Periods).Last();

                if (lastEma < 0 && ema.Ema.Value > AvgChange) finalResult = OrderSide.Buy;
                else if (lastEma > 0 && ema.Ema.Value < -AvgChange) finalResult = OrderSide.Sell;

                if (finalResult.HasValue)
                {
                    Log($"Status: cross: {cross}, lastEma: {lastEma}, ema: {ema.Ema} split: {AvgChange}", LogLevel.Debug, t);
                }

                if (lastEma == 0) lastEma = ema.Ema.Value;

                lastEma = finalResult.HasValue ? 0 : lastEma;
            }           

            return new SignalResultX(this)
            {
                finalResult = finalResult
            };

            //if (lastCrossValue == 0 && cross !=  0) lastCrossValue = cross;

            //Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
            //if (lastCrossValue > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
            //else if (lastCrossValue < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

            //lastCrossValue = finalResult.HasValue ? 0 : lastCrossValue;



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