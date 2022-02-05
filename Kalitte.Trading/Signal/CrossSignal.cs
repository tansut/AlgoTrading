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
    //public enum SignalConfirmStatus
    //{
    //    None,
    //    Verifying,
    //    Verified
    //}

    public class CrossSignal : Signal
    {
        //private volatile SignalConfirmStatus evalState = SignalConfirmStatus.None;
        private OrderSide? LastPeriodSignal = null;
        public IIndicator i1 = null;
        public IIndicator i2 = null;

        Task<OrderSide?> signalVerificationTask = null;
        CancellationTokenSource tokenSource2;
        CancellationToken ct;

        //private OrderSide? firstSignal = null;

        public decimal AvgChange = 0.3M;
        public int Periods = 5;
        //public decimal Moment = 0.3M;
        //public decimal PriceSplit = 0.3M;


        //private List<decimal> lastSignals = new List<decimal>();
        //private OrderSide? initialPeriodSignal = null;

        private TopQue i1List;
        private TopQue i2List;
        private TopQue priceList;




        public CrossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, IIndicator i1, IIndicator i2) : base(name, symbol, owner)
        {
            this.i1 = i1;
            this.i2 = i2;
        }

        private void updateList(List<decimal> l, decimal v)
        {
            l.Add(v);
            if (l.Count > Periods) l.RemoveAt(0);
        }

        private decimal deriv(List<decimal> list)
        {
            decimal dif = (list.Last() - list.First()) / Periods;
            return dif;
        }


        public override void Start()
        {
            LastPeriodSignal = null;            
            base.Start();
        }

        private void clearLists()
        {
            //i1List = new TopQue(Periods);
            //i2List = new TopQue(Periods);
            priceList = new TopQue(Periods);
        }

        public void CancelVerificationTask()
        {
            if (signalVerificationTask != null)
            {
                tokenSource2.Cancel();
                try
                {
                    signalVerificationTask.Wait();

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

                i1List = new TopQue(Periods);
                i2List = new TopQue(Periods);

                while (i1List.Count < Periods)
                {                    
                    i1List.Push(i1.CurrentValue);
                    i2List.Push(i2.CurrentValue);
                    
                    decimal avgEmaDif = i1List.ExponentialMovingAverage - i2List.ExponentialMovingAverage;

                    Algo.Log($"[{this.Name}]: Collecting {i1List.Count}. data [{avgEmaDif}] to start verifying {periodSignal} signal against {AvgChange}", LogLevel.Debug, t);

                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }
                    
                    if (i1List.Count == Periods)
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
                    
                    Thread.Sleep(Simulation ? 0:1000);
                }

                return finalResult;
            });

            signalVerificationTask.Start();
        }



        private OrderSide? getPeriodSignal(DateTime? t = null)
        {
            if (Algo.CrossAboveX(i1, i2, t)) return OrderSide.Buy;
            else if (Algo.CrossBelowX(i1, i2, t)) return OrderSide.Sell;
            return null;
        }

        private bool verifyPeriodSignal(OrderSide? received, DateTime? t = null)
        {
            Thread.Sleep(250);
            if (getPeriodSignal(t) != received) return false;
            return true;
        }


        protected SignalResultX CalculateSignal(DateTime? t = null)
        {
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
            //if (current.finalResult == LastSignal) return new SignalResultX(this) { finalResult = null };
            return current;
        }
    }

}