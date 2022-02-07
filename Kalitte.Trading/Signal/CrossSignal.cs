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
        private OrderSide? LastVerifiedSignal = null;
        public IIndicator i1 = null;
        public IIndicator i2 = null;


        public Kalitte.Trading.Indicators.IndicatorBase i1k;
        public Kalitte.Trading.Indicators.IndicatorBase i2k;


        Task<OrderSide?> signalVerificationTask = null;

        CancellationTokenSource tokenSource2;

        public decimal AvgChange = 0.3M;
        public int Periods = 5;
        private Bars bars;
        //bool useMyCross = false;
        bool useMyIndicators = true;

        private decimal lastCrossValue = 0;
        private decimal lastEma = 0;



        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        public override void Start()
        {
            LastVerifiedSignal = null;
            bars = new Bars(Periods);
            base.Start();
            Algo.Log($"{this.Name} started with {i1.GetType().Name}[{i1.Period}]/{i2.GetType().Name}[{i2.Period}] period: {Periods} avgChange: {AvgChange}");
        }

        public override void Stop()
        {
            if (signalVerificationTask != null) CancelVerificationTask();
            base.Stop();
        }


        protected override void Colllect()
        {





            //var list = string.Join(",", bars.List.Select(p => p.Close.ToString()));
            //Algo.Log($"{list}", LogLevel.Debug);
        }


        public void CancelVerificationTask()
        {
            if (signalVerificationTask != null)
            {
                tokenSource2.Cancel();
                try
                {
                    signalVerificationTask.Wait(tokenSource2.Token);

                }
                catch (OperationCanceledException)
                {

                }
                finally
                {
                    signalVerificationTask = null;
                    tokenSource2.Dispose();
                    tokenSource2 = null;
                }
            }
        }

        //public void StartVerificationTask(OrderSide periodSignal, DateTime? t)
        //{

        //    tokenSource2 = new CancellationTokenSource();
        //    Algo.Log($"[{this.Name}] creating verifiation task for {periodSignal}", LogLevel.Debug, t);



        //    signalVerificationTask = new Task<OrderSide?>(() =>
        //    {
        //        tokenSource2.Token.ThrowIfCancellationRequested();
        //        OrderSide? finalResult = null;

        //        var difList = new Bars(Periods);

        //        while (!tokenSource2.Token.IsCancellationRequested && difList.Count < Periods)
        //        {


        //            difList.Push(new Quote(DateTime.Now, i1.CurrentValue - i2.CurrentValue));

        //            decimal avgEmaDif = difList.Ema().Last();

        //            Algo.Log($"[{this.Name}/{Thread.CurrentThread.ManagedThreadId}]: Collecting {difList.Count}. data [{avgEmaDif}] to start verifying {periodSignal} signal against {AvgChange}", LogLevel.Debug, t);


        //            if (difList.Count == Periods)
        //            {
        //                if (avgEmaDif > AvgChange) finalResult = OrderSide.Buy;
        //                else if (avgEmaDif < -AvgChange) finalResult = OrderSide.Sell;

        //                if (finalResult.HasValue)
        //                {
        //                    if (finalResult != periodSignal)
        //                    {
        //                        Algo.Log($"[{this.Name}/{Thread.CurrentThread.ManagedThreadId}]: Tried to verify {periodSignal} but ended with {finalResult}. Resetting.", LogLevel.Warning, t);
        //                        finalResult = null;
        //                        LastVerifiedSignal = null;
        //                    }
        //                    else
        //                    {
        //                        Algo.Log($"[{this.Name}/{Thread.CurrentThread.ManagedThreadId}]: {periodSignal} verified with {avgEmaDif} EMA dif against {AvgChange}", LogLevel.Debug, t);
        //                    }
        //                }
        //                else
        //                {
        //                    Algo.Log($"[{this.Name}{Thread.CurrentThread.ManagedThreadId}]: Still trying to verify {periodSignal} signal with {avgEmaDif} EMA dif against {AvgChange}", LogLevel.Debug, t);
        //                }
        //            }

        //            Thread.Sleep(Simulation ? 0 : 1000);
        //        }

        //        return finalResult;
        //    });

        //    signalVerificationTask.Start();
        //}

        //private bool CrossAbove(DateTime? t)
        //{
        //    return useMyCross ? bars.Cross(0) > 0 : Algo.CrossAboveX(i1, i2, t);
        //}

        //private bool CrossBelow(DateTime? t)
        //{
        //    return useMyCross ? bars.Cross(0) < 0 : Algo.CrossBelowX(i1, i2, t);

        //}

        //private OrderSide? getPeriodSignal(DateTime? t = null)
        //{
        //    if (CrossAbove(t)) return OrderSide.Buy;
        //    else if (CrossBelow(t)) return OrderSide.Sell;
        //    return null;
        //}

        //private bool verifyPeriodSignal(OrderSide? received, DateTime? t = null)
        //{
        //    if (useMyCross) return true;
        //    Thread.Sleep(250);
        //    if (getPeriodSignal(t) != received) return false;
        //    return true;
        //}


        protected SignalResultX CalculateSignal(DateTime? t = null)
        {

            OrderSide? finalResult = null;



            //Colllect();



            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0)
            {

                var val = useMyIndicators ? i1k.LastValue(mp) - i2k.LastValue(mp) : i1.CurrentValue - i2.CurrentValue;
                bars.Push(new Quote(val));

                Algo.Log($"i1k: {i1k.LastValue(mp)} i1:{i1.CurrentValue} mp: {mp}");
                Algo.Log($"i2k: {i2k.LastValue(mp)} i2:{i2.CurrentValue} mp: {mp}");

                var cross = bars.Cross(0);
                var ema = bars.Ema().Last();


                if (lastEma < 0 && ema > AvgChange) finalResult = OrderSide.Buy;
                else if (lastEma > 0 && ema < -AvgChange) finalResult = OrderSide.Sell;

                if (lastEma == 0) lastEma = ema;

                lastEma = finalResult.HasValue ? 0 : lastEma;

                Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, lastEma: {lastEma}, ema: {ema} period: {bars.Count} split: {AvgChange}", LogLevel.Debug, t);

                //Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} cross: {cross}, ema: {ema}", LogLevel.Debug, t);
            }
            else Algo.Log($"{this.Name}/{Thread.CurrentThread.ManagedThreadId} no market price for {t}");




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

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var current = CalculateSignal(t);
            return current;
        }
    }
}