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
        private Bars bars;
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
            bars = new Bars(Periods);
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

            var val = i1.CurrentValue - i2.CurrentValue;

            bars.Push(new Quote(val));

            var cross = bars.Cross(0);
            var ema = bars.Ema().Last();

            if (lastEma < 0 && ema > AvgChange) finalResult = OrderSide.Buy;
            else if (lastEma > 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

            if (lastEma == 0) lastEma = ema;

            lastEma = finalResult.HasValue ? 0 : lastEma;

            if (!Simulation) Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);








            //if (lastCrossValue == 0 && cross !=  0) lastCrossValue = cross;

            //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
            //if (lastCrossValue > 0 && ema > AvgChange) finalResult = OrderSide.Buy;
            //else if (lastCrossValue < 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

            //lastCrossValue = finalResult.HasValue ? 0 : lastCrossValue;



            //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);
            //var changedDirection = lastEma * 




            return new SignalResultX(this)
            {
                finalResult = finalResult
            };


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