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
    public enum SignalConfirmStatus
    {
        None,
        Verifying,
        Verified
    }

    public class CrossSignal : Signal
    {
        private SignalConfirmStatus evalState = SignalConfirmStatus.None;
        private OrderSide? LastPeriodSignal = null;
        public IIndicator i1 = null;
        public IIndicator i2 = null;

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

        //private OrderSide? getFinalResult()
        //{
        //    if (lastSignals.Count < Periods) return null;
        //    OrderSide? result = null;


        //    //if (avgDif > AvgChange) result = OrderSide.Buy;
        //    //else if (avgDif < -AvgChange && d2 < 0) result = OrderSide.Sell;






        //    //decimal d1 = deriv(i1List), d2 = deriv(i2List), dp = deriv(priceList);
        //    //decimal d12 = d1 - d2;
        //    //decimal difi1First = i1List.First() - i2List.First();
        //    //decimal difi1Last = i1List.Last() - i2List.Last();

        //    //Algo.Debug($"avgp: {priceList.Average()} d1: {d1} d2: {d2} dp: {dp} d1-d2: {d12} split: {AvgChange}");



        //    //if (difi1First < 0M && difi1Last > 0m) // crossover
        //    //{
        //    //    result = OrderSide.Buy;
        //    //}
        //    //else if (difi1First > 0M && difi1Last > 0m)
        //    //{
        //    //    result = OrderSide.Buy;
        //    //}



        //    //return result;

        //    //var avg = lastSignals.Average();
        //    //if (avg >= Split) finalResult = OrderSide.Buy;
        //    //else if (avg <= -Split) finalResult = OrderSide.Sell;
        //    //else Algo.Log($"Average: {avg} slip: {Split} price:{Algo.GetMarketPrice(Symbol, t)}", LogLevel.Debug);

        //}

        public override void Start()
        {
            Reset();
            base.Start();
        }

        private void clearLists()
        {
            i1List = new TopQue(Periods);
            i2List = new TopQue(Periods);
            priceList = new TopQue(Periods);            
        }

        private void Reset()
        {
            clearLists();
            LastPeriodSignal = null;
            evalState = SignalConfirmStatus.None;
        }

        private OrderSide? getPeriodSignal(DateTime? t = null)
        {
            if (Algo.CrossAboveX(i1, i2, t)) return OrderSide.Buy;
            else if (Algo.CrossBelowX(i1, i2, t)) return OrderSide.Sell;
            return null;
        }

        private bool verifyPeriodSignal(OrderSide? received, DateTime? t = null)
        {
            Thread.Sleep(500);
            if (getPeriodSignal(t) != received) return false;
            return true;
        }


        protected SignalResultX CalculateSignal(DateTime? t = null)
        {

            OrderSide? periodSignal = null;
            OrderSide? finalResult = null;



            periodSignal = getPeriodSignal(t);
           

            if (Simulation)
            {
                return new SignalResultX(this) { finalResult = periodSignal };
            }

            if (!verifyPeriodSignal(periodSignal, t)) return null;

            if (periodSignal != LastPeriodSignal)
            {
                if (LastPeriodSignal.HasValue && !periodSignal.HasValue)
                {
                    periodSignal = LastPeriodSignal.Value;
                    if (evalState != SignalConfirmStatus.Verified)
                    {
                        Algo.Log($"Received empty period signal, reverifying previous signal {LastPeriodSignal}.", LogLevel.Warning);
                        evalState = SignalConfirmStatus.Verifying;
                    }
                    else
                    {
                        //Algo.Log($"Received empty period signal but will use last verified signal {LastPeriodSignal} ", LogLevel.Debug);
                    }
                }
                else
                {
                    Algo.Log($"Period signal changed from {LastPeriodSignal} to {periodSignal}", LogLevel.Debug);
                    Reset();
                    LastPeriodSignal = periodSignal;
                    evalState = periodSignal.HasValue ? SignalConfirmStatus.Verifying : evalState;
                }
            }

            if (evalState == SignalConfirmStatus.Verifying)
            {
                i1List.Push(i1.CurrentValue);
                i2List.Push(i2.CurrentValue);
                //ateList(priceList, Algo.GetMarketPrice(Symbol, t));
                decimal avgDif = i1List.Average() - i2List.Average();
                decimal avgEmaDif = i1List.ExponentialMovingAverage - i2List.ExponentialMovingAverage;

                if (i1List.Count >= Periods)
                {
                    if (avgEmaDif > AvgChange) finalResult = OrderSide.Buy;
                    else if (avgEmaDif < -AvgChange) finalResult = OrderSide.Sell;

                    if (finalResult.HasValue)
                    {
                        if (finalResult != periodSignal)
                        {
                            Algo.Log($"[{this.Name}]: Tried to verify {periodSignal} but ended with {finalResult}. Resetting.", LogLevel.Info);
                            this.Reset();
                        }
                        else
                        {
                            Algo.Log($"[{this.Name}]: {periodSignal} verified with {avgDif} {avgEmaDif} {AvgChange}", LogLevel.Debug);
                            evalState = SignalConfirmStatus.Verified;
                        }
                    }
                    else
                    {
                        Algo.Log($"[{this.Name}]: Still trying to verify {periodSignal} signal. Avg: {avgDif} {avgEmaDif} {AvgChange}");
                    }
                }
                else Algo.Log($"[{this.Name}]: Collecting data to start verifying {periodSignal} signal. Avg: {avgDif} {avgEmaDif} {AvgChange}");
            }

            return new SignalResultX(this) { finalResult = (evalState == SignalConfirmStatus.Verified ? periodSignal : null) };


            //periodSignal = OrderSide.Sell;
            //Algo.Log($"cacl initial period {initialPeriodSignal}", LogLevel.Debug);

            //if (!initialPeriodSignal.HasValue && periodSignal.HasValue)
            //{
            //    initialPeriodSignal = periodSignal;
            //    Algo.Log($"Setting initial period signal to {initialPeriodSignal}", LogLevel.Debug);
            //}

            //if (CheckCount == 0 && !initialPeriodSignal.HasValue)
            //{
            //    Algo.Log($"No initial signal for {Name}. Suspended until a valid initial signal", LogLevel.Warning);
            //}

            //if (!initialPeriodSignal.HasValue) return new SignalResultX(this) { finalResult = null };

            //if (i1.CurrentValue > i2.CurrentValue) currentSignal = OrderSide.Buy;
            //else if (i1.CurrentValue < i2.CurrentValue) currentSignal = OrderSide.Sell;



            //var dif = i1.CurrentValue - i2.CurrentValue;
            //Algo.Log($"{i1.CurrentValue} - {i2.CurrentValue}", LogLevel.Debug);




        }

        protected override SignalResultX CheckInternal(DateTime? t = null)
        {
            var current = CalculateSignal(t);
            //if (current.finalResult == LastSignal) return new SignalResultX(this) { finalResult = null };
            return current;
        }
    }

}