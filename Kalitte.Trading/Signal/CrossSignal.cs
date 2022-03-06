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
    public class Sensitivity
    {
        public decimal VolumePower { get; set; }
        public DataTime VolumeTime { get; set; }
        public decimal VolumeRatio { get; set; }
        public decimal TrendRatio { get; set; }
        public decimal Result { get; set; }

        public override string ToString()
        {
            return $"ratio: {Result} volumePower: {VolumePower}[{VolumeTime}] ratioByVolume: {VolumeRatio} ratioByTrend: {TrendRatio}";
        }
    }

    public class CrossSignalResult : SignalResult
    {
        public decimal i1Val { get; set; }
        public decimal i2Val { get; set; }
        public decimal Dif { get; set; }
        public Sensitivity Sensitivity { get; set; }
        public bool MorningSignal { get; set; } = false;

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

        private FinanceList<decimal> crossBars;


        private decimal lastCross = 0;
        public Sensitivity LastCalculatedSensitivity { get; set; }

        public CrossSignal(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {

        }



        protected override void ResetInternal()
        {
            crossBars.Clear();
            lastCross = 0;
            AvgChange = InitialAvgChange;
            base.ResetInternal();
        }

        public void ResetCross()
        {
            InOperationLock.WaitOne();
            InOperationLock.Reset();
            try
            {
                crossBars.Clear();
                lastCross = 0;
            }
            finally
            {
                InOperationLock.Set();
            }
        }

        public override void Init()
        {
            this.InitialAvgChange = AvgChange;
            crossBars = new FinanceList<decimal>(100);
            this.Indicators.Add(i1k);
            this.Indicators.Add(i2k);
            this.i1k.InputBars.ListEvent += base.InputbarsChanged;
            MonitorInit("sensitivity/volumePower", 0);
            MonitorInit("sensitivity/avgchange", AvgChange);
            MonitorInit("sensitivity/trendRatio", 0);
            base.Init();
        }


        protected override void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {
            AnalyseList.Clear();
        }


        protected override void AdjustSensitivityInternal(double ratio, string reason)
        {
            AvgChange = InitialAvgChange + (InitialAvgChange * (decimal)ratio);
            Monitor("sensitivity/avgchange", AvgChange);
            base.AdjustSensitivityInternal(ratio, reason);
        }

        public void AdjustSensitivity(double ratio, string reason)
        {
            InOperationLock.WaitOne();
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
            return $"{base.ToString()}: {i1k.ToString()}/{i2k.ToString()}] period: {AnalyseSize} pricePeriod: {CollectSize}  avgChange: {AvgChange}";
        }

        private void applySensitivity(Sensitivity sensitivity)
        {
            if (sensitivity != null)
            {
                //if (sensitivity == null) AdjustSensitivity(0, "reverted");
                AdjustSensitivityInternal((double)sensitivity.Result, "Calculation");
                LastCalculatedSensitivity = sensitivity;
            }

        }

        private Sensitivity CalculateSensitivity()
        {
            var result = new Sensitivity();

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
                var da = Math.Abs(((dl + d) / 2).Value);

                var max = InitialAvgChange * 1M;

                var powerRatio = 0M;
                var powerNote = "";
                decimal usedPower = 0;

                if (PowerSignal != null)
                {
                    var instantPower = PowerSignal.LastSignalResult as PowerSignalResult;
                    var barPower = PowerSignal.Indicator.Results.Last();
                    result.VolumeTime = instantPower != null && instantPower.Value > 0 ? DataTime.Current : DataTime.LastBar;
                    usedPower = result.VolumeTime == DataTime.Current ? instantPower.Value : barPower.Value.Value;
                    powerRatio = (PowerCrossThreshold - usedPower) / 100;
                    powerRatio = powerRatio > 0 ? powerRatio * PowerCrossPositiveMultiplier : powerRatio * PowerCrossNegativeMultiplier;
                    powerNote = $"bar: {barPower.Date} rsiBar: {barPower.Value} rsiInstant: {(instantPower == null ? 0 : instantPower.Value)}";
                    result.VolumePower = usedPower;
                    result.VolumeRatio = powerRatio;
                }

                var dtRatio = 0M;

                if (dt < max && da < max)
                {
                    dtRatio = ((max - dt) / max);
                }


                var divide = 0;
                if (dtRatio != 0) divide++;
                if (powerRatio != 0) divide++;
                var average = divide > 0 ? (powerRatio + dtRatio) / divide : 0;

                if (Algo.IsMorningStart() && lastCross != 0)
                {
                    var difRatio =  (double)(Math.Abs(lastCross) / InitialAvgChange);
                    if (difRatio > 2.0)
                    {
                        average = -(decimal)(1 / (1 + Math.Pow(Math.E, -(1*difRatio))));
                    }
                }

                Monitor("sensitivity/trendRatio", dtRatio);
                Monitor("sensitivity/volumePower", usedPower);
                result.TrendRatio = dtRatio;
                result.Result = average;
            }
            catch (Exception exc)
            {
                Log($"Error in calculating sensitivity. {exc.Message} {exc.StackTrace}", LogLevel.Error);
                return null;
            }
            return result;
        }

        private void FillMorningCross(DateTime time)
        {
            if (crossBars.Count == 0)
            {
                var last = i1k.Results.Last().Date;

                if (last.Hour == 22 && last.Minute == 50)
                {
                    var l1 = i1k.Results.Last();
                    var l2 = i2k.Results.Last();
                    crossBars.Push(l1.Value.Value - l2.Value.Value);
                    Log($"Morning cross inserted: {l1.Date}", LogLevel.Debug);
                }
            }
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? DateTime.Now;
            var result = new CrossSignalResult(this, t ?? DateTime.Now);
            result.MorningSignal = Algo.IsMorningStart(time);
            if (result.MorningSignal) FillMorningCross(time);

            if (DynamicCross)
            {
                var sensitivity = CalculateSensitivity();
                applySensitivity(sensitivity);
                result.Sensitivity = sensitivity;
            }

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp);

            if (CollectList.Ready && mp >= 0)
            {
                decimal mpAverage = CollectList.LastValue;

                var l1 = i1k.NextValue(mpAverage).Value.Value;
                var l2 = i2k.NextValue(mpAverage).Value.Value;

                AnalyseList.Collect(l1 - l2);
                crossBars.Push(l1 - l2);
                var cross = Helper.Cross(crossBars.ToArray, 0);

                if (lastCross == 0 && cross != 0)
                {
                    lastCross = cross;
                    Log($"Cross identified: {lastCross}", LogLevel.Debug, t);
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

                }

            }





            return result;
        }


    }
}