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
        private OrderSide? LastPeriodSignal = null;
        public IIndicator i1 = null;
        public IIndicator i2 = null;

        Task<OrderSide?> signalVerificationTask = null;
        CancellationTokenSource tokenSource2;

        public decimal AvgChange = 0.3M;
        public int Periods = 5;
        private Bars bars;
        bool useMyCross = true;


        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        public override void Start()
        {
            LastPeriodSignal = null;
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
            bars.Push(new Quote(i1.CurrentValue - i2.CurrentValue));
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

        public void StartVerificationTask(OrderSide periodSignal, DateTime? t)
        {

            tokenSource2 = new CancellationTokenSource();
            Algo.Log($"[{this.Name}] creating verifiation task for {periodSignal}", LogLevel.Debug, t);

            

            signalVerificationTask = new Task<OrderSide?>(() =>
            {
                tokenSource2.Token.ThrowIfCancellationRequested();
                OrderSide? finalResult = null;

                var difList = new Bars(Periods);

                while (!tokenSource2.Token.IsCancellationRequested && difList.Count < Periods)
                {


                    difList.Push(new Quote(DateTime.Now, i1.CurrentValue - i2.CurrentValue));

                    decimal avgEmaDif = difList.Ema().Last();

                    Algo.Log($"[{this.Name}]: Collecting {difList.Count}. data [{avgEmaDif}] to start verifying {periodSignal} signal against {AvgChange}", LogLevel.Debug, t);


                    if (difList.Count == Periods)
                    {
                        if (avgEmaDif > AvgChange) finalResult = OrderSide.Buy;
                        else if (avgEmaDif < -AvgChange) finalResult = OrderSide.Sell;

                        if (finalResult.HasValue)
                        {
                            if (finalResult != periodSignal)
                            {
                                Algo.Log($"[{this.Name}]: Tried to verify {periodSignal} but ended with {finalResult}. Resetting.", LogLevel.Warning, t);
                                finalResult = null;
                                LastPeriodSignal = null;
                            }
                            else
                            {
                                Algo.Log($"[{this.Name}]: {periodSignal} verified with {avgEmaDif} EMA dif against {AvgChange}", LogLevel.Debug, t);
                            }
                        }
                        else
                        {
                            Algo.Log($"[{this.Name}]: Still trying to verify {periodSignal} signal with {avgEmaDif} EMA dif against {AvgChange}", LogLevel.Debug, t);
                        }
                    }

                    Thread.Sleep(Simulation ? 0 : 1000);
                }

                return finalResult;
            });

            signalVerificationTask.Start();
        }

        private bool CrossAbove(DateTime? t)
        {
            return useMyCross ? bars.Cross(0) > 0 : Algo.CrossAboveX(i1, i2, t);
        }

        private bool CrossBelow(DateTime? t)
        {
            return useMyCross ? bars.Cross(0) < 0 : Algo.CrossBelowX(i1, i2, t);

        }

        private OrderSide? getPeriodSignal(DateTime? t = null)
        {
            if (CrossAbove(t)) return OrderSide.Buy;
            else if (CrossBelow(t)) return OrderSide.Sell;
            return null;
        }

        private bool verifyPeriodSignal(OrderSide? received, DateTime? t = null)
        {
            if (useMyCross) return true;
            Thread.Sleep(250);
            if (getPeriodSignal(t) != received) return false;
            return true;
        }


        protected SignalResultX CalculateSignal(DateTime? t = null)
        {
            Colllect();

            OrderSide? periodSignal = null;            
            periodSignal = getPeriodSignal(t);
            
            var verifySignal = false;

            if (!Simulation && !verifyPeriodSignal(periodSignal, t)) return null;

            if (periodSignal != LastPeriodSignal)
            {
                if (LastPeriodSignal.HasValue && !periodSignal.HasValue)
                {
                    periodSignal = LastPeriodSignal.Value;
                    if (signalVerificationTask == null || !signalVerificationTask.Result.HasValue)
                    {
                        Algo.Log($"Received empty period signal, reverifying previous signal {LastPeriodSignal}.", LogLevel.Warning, t);
                        if (signalVerificationTask != null) CancelVerificationTask();
                        verifySignal = true;
                    }
                }
                else
                {
                    Algo.Log($"Period signal changed from {LastPeriodSignal} to {periodSignal}", LogLevel.Debug, t);
                    if (signalVerificationTask != null) CancelVerificationTask();
                    LastPeriodSignal = periodSignal;
                    if (periodSignal.HasValue) verifySignal = true;
                }
            }

            if (verifySignal && signalVerificationTask == null) StartVerificationTask(periodSignal.Value, t);

            if (Simulation && signalVerificationTask != null && !signalVerificationTask.IsCompleted) signalVerificationTask.Wait();

            return new SignalResultX(this) { finalResult = (signalVerificationTask != null && signalVerificationTask.IsCompleted ? signalVerificationTask.Result : null) };
        }

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var current = CalculateSignal(t);            
            return current;
        }
    }
}