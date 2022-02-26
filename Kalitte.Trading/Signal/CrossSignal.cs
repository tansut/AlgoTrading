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
using Kalitte.Trading.Algos;

namespace Kalitte.Trading
{


    public class CrossSignalResult : SignalResult
    {
        public decimal i1Val { get; set; }
        public decimal i2Val { get; set; }
        public decimal Dif { get; set; }

        public CrossSignalResult(Signal signal, DateTime t) : base(signal, t)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()} | i1:{i1Val} i2:{i2Val} dif:{Dif}";
        }
    }

    public class CrossSignal : AnalyserBase
    {
        public bool DynamicCross { get; set; } = false;
        public PowerSignal PowerSignal { get; set; }
        public decimal PowerCrossThreshold { get; set; }
        public decimal PowerCrossNegativeMultiplier { get; set; }
        public decimal PowerCrossPositiveMultiplier { get; set; }

        public ITechnicalIndicator i1k;
        public ITechnicalIndicator i2k;

        public decimal AvgChange = 0.3M;
        public decimal InitialAvgChange;
        public int InitialPeriods;

        //private FinanceList<IQuote> differenceBars;
        private FinanceList<decimal> crossBars;


        private decimal lastCross = 0;
        public bool UseSma = true;


        public CrossSignal(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {

        }



        protected override void ResetInternal()
        {
            crossBars.Clear();
            lastCross = 0;
            AnalyseSize = InitialPeriods;
            AvgChange = InitialAvgChange;
            base.ResetInternal();
        }





        public override void Init()
        {
            this.InitialPeriods = AnalyseSize;
            this.InitialAvgChange = AvgChange;
            crossBars = new FinanceList<decimal>(AnalyseSize);            
            this.Indicators.Add(i1k);
            this.Indicators.Add(i2k);
            this.i1k.InputBars.ListEvent += base.InputbarsChanged;
            base.Init();
        }


        protected override void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {
            AnalyseList.Clear();
        }


        protected void AdjustSensitivityInternal(double ratio, string reason)
        {
            AvgChange = InitialAvgChange + (InitialAvgChange * (decimal)ratio);
            AnalyseSize = InitialPeriods + Convert.ToInt32((InitialPeriods * (decimal)ratio));
            AnalyseList.Resize(AnalyseSize);
            crossBars.Resize(AnalyseSize);
            Log($"{reason}: Adjusted to (%{((decimal)ratio * 100).ToCurrency()}): {AvgChange}, {AnalyseSize}", LogLevel.Debug);
        }

        public void AdjustSensitivity(double ratio, string reason)
        {
            //InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                AdjustSensitivityInternal(ratio, reason);
            }
            finally
            {
                InOperationLock.Set();
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {i1k.ToString()}/{i2k.ToString()}] period: {AnalyseSize} pricePeriod: {CollectSize} useSma: {UseSma} avgChange: {AvgChange}";
        }


        private void CalculateSensitivity()
        {
            try
            {
                var b12 = i1k.Results[i1k.Results.Count - 2];
                var b22 = i2k.Results[i2k.Results.Count - 2];


                var rl1 = b12.Value.Value;
                var rl2 = b22.Value.Value;

                var b1 = i1k.Results.Last();
                var b2 = i2k.Results.Last();

                var r1 = b1.Value;
                var r2 = b2.Value;

                var dl = rl1 - rl2;
                var d = r1 - r2;

                var dt = Math.Abs((dl - d).Value);

                var max = InitialAvgChange * 1M;

                var powerRatio = 0M;

                var powerNote = "";

                if (PowerSignal != null)
                {
                    var instantPower = PowerSignal.LastSignalResult as PowerSignalResult;
                    //var rsiIndicator = PowerSignal.Indicator as Rsi;
                    //var lastBar = rsiIndicator
                    var barPower = PowerSignal.Indicator.Results.Last();
                    var usedPower = instantPower != null && instantPower.Value > 0 ? instantPower.Value : barPower.Value.Value;
                    powerRatio = (PowerCrossThreshold - usedPower) / 100;
                    powerRatio = powerRatio > 0 ? powerRatio * PowerCrossPositiveMultiplier : powerRatio * PowerCrossNegativeMultiplier;
                    powerNote = $"bar: {barPower.Date} rsiBar: {barPower.Value} rsiInstant: {(instantPower == null ? 0 : instantPower.Value)}";
                }

                var dtRatio = 0M;

                if (dt < max)
                {
                    dtRatio = ((max - dt) / max);
                }

                var divide = 0;
                if (dtRatio != 0) divide++;
                if (powerRatio != 0) divide++;

                var average = divide > 0 ? (powerRatio + dtRatio) / divide : 0;
                AdjustSensitivity((double)average, $"Bars: [{b12.Date} - {b1.Date}] power: {powerRatio} [{powerNote}] dt:{dtRatio}  result:{average}");

            }
            catch (Exception exc)
            {
                Log($"Error in calculating sensitivity. {exc.Message} {exc.StackTrace}", LogLevel.Error);
            }
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? DateTime.Now;

            if (DynamicCross) CalculateSensitivity();

            var result = new CrossSignalResult(this, t ?? DateTime.Now);
            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            if (CollectList.Ready && mp >= 0)
            {

                decimal mpAverage = CollectList.LastValue;

                var l1 = i1k.NextValue(mpAverage);
                var l2 = i2k.NextValue(mpAverage);

                AnalyseList.Collect(l1 - l2);
                crossBars.Push(l1 - l2);
                var cross = Helper.Cross(crossBars.ToArray, 0);

                if (lastCross == 0 && cross != 0)
                {
                    lastCross = cross;
                    Log($"Cross identified: {cross}", LogLevel.Debug, t);
                    AnalyseList.Clear();
                }

                if (AnalyseList.Ready)
                {

                    var lastAvg = AnalyseList.LastValue; // UseSma ? differenceBars.List.GetSma(AnalyseSize).Last().Sma.Value : differenceBars.List.GetEma(AnalyseSize).Last().Ema.Value;

                    decimal last1 = i1k.Results.Last().Value.Value;
                    decimal last2 = i2k.Results.Last().Value.Value;

                    result.i1Val = last1;
                    result.i2Val = last2;
                    result.Dif = lastAvg;

                    if (lastCross != 0 && lastAvg > AvgChange) result.finalResult = BuySell.Buy;
                    else if (lastCross != 0 && lastAvg < -AvgChange) result.finalResult = BuySell.Sell;

                    if (result.finalResult.HasValue)
                    {
                        AnalyseList.Clear();
                    }

                }

            }





            return result;
        }
    }
}