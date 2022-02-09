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
            Algo.Log($"{this.Name} started with {i1.GetType().Name}[{i1.Period}]/{i2.GetType().Name}[{i2.Period}] period: {Periods} avgChange: {AvgChange}");
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
            var mp = Algo.GetMarketPrice(Symbol, t); // düşün  mp 0 gelirse

            if (mp == 0)
            {
                mp = i1k.InputBars.Latest.Close;
                Algo.Log($"Used last close bar price { mp }", LogLevel.Warning, t);
            }
            decimal i1Val = i1.CurrentValue, i2Val = i2.CurrentValue;
            Algo.Log($"bar last: {(i1k.HasResult ? i1k.ResultBars.List.Last().Close : 0)}");

            var l1 = i1k.CreateNewResultBar(new Quote(mp));
            var l2 = i2k.CreateNewResultBar(new Quote(mp));

            var emaTest= (decimal)i1k.InputBars.EmaNext((double)mp, (double)i1k.InputBars.List.Last().Close, 5);

            Algo.Log($"l1: {l1.Close} l2: {l2.Close} test: emates: {emaTest} mp: {(double)mp} lastema: {((double)i1k.InputBars.List.Last().Close)}");

            //differenceBars.Push(new Quote(i1Val-i2Val));
            var newResultBar = new Quote(t ?? DateTime.Now, l1.Close - l2.Close);
            Algo.Log($"added new item {newResultBar}");
            differenceBars.Push(newResultBar);
            if (mp > 0) priceBars.Push(new Quote(t ?? DateTime.Now, mp));


            if (differenceBars.Count >= Periods && priceBars.Count >= 2)
            {
                //var mpEma = priceBars.Ema(2).Last().Ema.Value;


                Algo.Log($"l1: {l1.Close}  i1: {l2.Close} i2: {i2Val} l2: {i1Val}");

                var ldif = Math.Round(l1.Close - l2.Close, 5);
                var idif = Math.Round(i1Val - i2Val, 5);


                Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} mp:{mp} t: {t} ldif: {ldif} idif: {idif} diff: {ldif - idif}", LogLevel.Error, t);

                var cross = differenceBars.Cross(0);
                var ema = differenceBars.Ema(Periods).Last();

                Algo.Log($" cross: {cross}, lastEma: {lastEma}, ema: {ema.Ema} split: {AvgChange}", LogLevel.Debug, t);


                if (lastEma < 0 && ema.Ema.Value > AvgChange) finalResult = OrderSide.Buy;
                else if (lastEma > 0 && ema.Ema.Value < -AvgChange) finalResult = OrderSide.Sell;

                if (lastEma == 0) lastEma = ema.Ema.Value;

                lastEma = finalResult.HasValue ? 0 : lastEma;

            }
            else Algo.Log($"Collected {differenceBars.Count} data ...");






            return new SignalResultX(this)
            {
                finalResult = finalResult
            };







            //if (lastCrossValue == 0 && cross !=  0) lastCrossValue = cross;

            //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
            //if (lastCrossValue > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
            //else if (lastCrossValue < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

            //lastCrossValue = finalResult.HasValue ? 0 : lastCrossValue;



            //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);
            //var changedDirection = lastEma * 







        }



        //protected SignalResultX CalculateSignal(DateTime? t = null)
        //{

        //    OrderSide? finalResult = null;

        //    var mp = Algo.GetMarketPrice(Symbol, t);

        //    if (mp == 0 && useLastPriceIfMissing)
        //    {
        //        Algo.Log($"No price was found, used last price {lastMarketPrice}", LogLevel.Warning, t);
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
        //            Algo.Log($"i1k: {i1k.LastValue(mp)} i1:{i1.CurrentValue} mp: {mp}", LogLevel.Debug, t);
        //            Algo.Log($"i2k: {i2k.LastValue(mp)} i2:{i2.CurrentValue} mp: {mp}", LogLevel.Debug, t);
        //        }

        //        var cross = bars.Cross(0);
        //        var ema = bars.Ema().Last();


        //        if (lastEma < 0 && ema > AvgChange) finalResult = OrderSide.Buy;
        //        else if (lastEma > 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

        //        if (lastEma == 0) lastEma = ema;

        //        lastEma = finalResult.HasValue ? 0 : lastEma;

        //        Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);

        //        //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
        //    }
        //    else Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} no market price for {t}", LogLevel.Debug, t);




        //    //if (lastCrossValue == 0 && cross !=  0) lastCrossValue = cross;

        //    //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
        //    //if (lastCrossValue > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
        //    //else if (lastCrossValue < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

        //    //lastCrossValue = finalResult.HasValue ? 0 : lastCrossValue;



        //    //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);
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