﻿// algo
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
using System.Drawing;
using Kalitte.Trading.Numeric;

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

    public enum CrossType
    {
        BeforeDown,
        AfterDown,
        BeforeUp,
        AfterUp
    }

    public class CrossSignalResult : SignalResult
    {
        public decimal i1Val { get; set; }
        public decimal i2Val { get; set; }
        public decimal Dif { get; set; }
        public decimal Rsi { get; set; }
        public decimal RsiOfRsi { get; set; }
        public bool RsiReady { get; set; }
        public Sensitivity Sensitivity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MarketPrice { get; set; }
        public bool MorningSignal { get; set; } = false;
        public CrossType? CrossType { get; set; }
        public decimal LastCross { get; set; }
        public decimal Cross { get; set; }


        public CrossSignalResult(SignalBase signal, DateTime t) : base(signal, t)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()} | i1:{i1Val} i2:{i2Val} dif:{Dif} ap:{AveragePrice} mp:{MarketPrice}";
        }

        public override int GetHashCode()
        {
            var hash = base.GetHashCode();
            hash = hash * 23 + CrossType.GetHashCode();
            return hash;
        }

    }

    public class CrossSignalConfig : AnalyserConfig
    {
        [AlgoParam(0)]
        public decimal PowerThreshold { get; set; }

        [AlgoParam(0)]
        public decimal PowerNegativeMultiplier { get; set; }

        [AlgoParam(0)]
        public decimal PowerPositiveMultiplier { get; set; }

        [AlgoParam(true)]
        public bool Dynamic { get; set; }

        [AlgoParam(0.3)]
        public decimal AvgChange { get; set; }

        [AlgoParam(0.3)]
        public decimal PreChange { get; set; }

    }

    public class CrossSignal : AnalyserBase<CrossSignalConfig>
    {

        public PowerSignal PowerSignal { get; set; }
        public decimal InitialCross { get; set; }


        public ITechnicalIndicator i1k;
        public ITechnicalIndicator i2k;

        public decimal AvgChange = 0.3M;
        public decimal PreChange = 0.1M;


        private FinanceList<decimal> crossBars;
        private List<MyQuote> closeWarmupList;
        private List<MyQuote> ohlcWarmupList;
        public AnalyseList RsiList { get; set; }
        public AnalyseList RsiOfRsiList { get; set; }

        //public bool FirstCrossRequired { get; set; } = true;

        public decimal LastCross { get; private set; } = 0;

        public CrossSignal(string name, string symbol, AlgoBase owner, CrossSignalConfig config) : base(name, symbol, owner, config)
        {
        }

        public Sensitivity LastCalculatedSensitivity { get; set; }
        public UKF Filter { get; set; }





        protected override void ResetInternal()
        {
            crossBars.Clear();
            closeWarmupList.Clear();
            ohlcWarmupList.Clear();
            Filter = new UKF();
            LastCross = 0;
            AvgChange = Config.AvgChange;
            PreChange = Config.PreChange;
            RsiList.Clear();
            RsiOfRsiList.Clear();
            base.ResetInternal();

            //TODO: Check for other signal
            //AnalyseList.Init(i1k.Results.Where(p => p.Value.HasValue).Select(p => MyQuote.Create(p.Date, p.Value.Value, OHLCType.Close)));
        }

        public void ResetCross()
        {
            Monitor.Enter(OperationLock);
            try
            {
                crossBars.Clear();
                LastCross = 0;
                Log($"Cross reset", LogLevel.Debug);
            }
            finally
            {
                Monitor.Exit(OperationLock);
            }
        }

        public override OrderUsage Usage { get => base.Usage == OrderUsage.Unknown ? OrderUsage.CreatePosition : base.Usage; protected set => base.Usage = value; }

        public override void Init()
        {
            AvgChange = Config.AvgChange;
            PreChange = Config.PreChange;
            crossBars = new FinanceList<decimal>(60 * 10);
            closeWarmupList = new List<MyQuote>();
            ohlcWarmupList = new List<MyQuote>();
            this.Indicators.Add(i1k);
            this.Indicators.Add(i2k);
            RsiList = new AnalyseList(Config.Lookback * 5, Average.Sma);
            RsiOfRsiList = new AnalyseList(Config.Lookback * 5, Average.Sma);
            RsiList.Period = Config.AnalysePeriod;
            RsiOfRsiList.Period = Config.AnalysePeriod;
            this.i1k.InputBars.ListEvent += base.InputbarsChanged;
            MonitorInit("sensitivity/volumePower", 0);
            MonitorInit("sensitivity/avgchange", AvgChange);
            MonitorInit("sensitivity/trendRatio", 0);
            base.Init();
        }

        public override void Start()
        {
            //if (AnalyseList.Count == 0)
            //{
            //    var lastBar = i1k.InputBars.Last.Date;
            //    Helper.SymbolSeconds(i1k.InputBars.Period.ToString(), out int seconds);
            //    var maxBar = lastBar.AddSeconds(seconds);
            //    //AnalyseList.Init(i1k.Results.Where(p => p.Value.HasValue).Select(p => MyQuote.Create(p.Date, p.Value.Value, OHLCType.Close)));
            //    //for(var t = lastBar; t < maxBar;t=t.AddSeconds(1))
            //    //{

            //    //}
            //}
            base.Start();
        }


        protected override void AdjustSensitivityInternal(double ratio, string reason)
        {
            AvgChange = Config.AvgChange + (Config.AvgChange * (decimal)ratio);
            Watch("sensitivity/avgchange", AvgChange);
            base.AdjustSensitivityInternal(ratio, reason);
        }



        public override string ToString()
        {
            return $"{base.ToString()}: {i1k.ToString()}/{i2k.ToString()}] period: {AnalyseSize} pricePeriod: {CollectSize}  avgChange: {AvgChange}";
        }

        private void applySensitivity(Sensitivity sensitivity)
        {
            if (sensitivity != null)
            {
                AdjustSensitivityInternal((double)sensitivity.Result, "Calculation");
                LastCalculatedSensitivity = sensitivity;
            }

        }

        private Sensitivity CalculateSensitivity()
        {
            var result = new Sensitivity();

            try
            {
                //var b12 = i1k.Results[i1k.Results.Count - 2];
                //var b22 = i2k.Results[i2k.Results.Count - 2];


                //var rl1 = b12.Value.Value;
                //var rl2 = b22.Value.Value;

                //var b1 = i1k.Results.Last();
                //var b2 = i2k.Results.Last();

                //var r1 = b1.Value;
                //var r2 = b2.Value;

                //var dl = rl1 - rl2;
                //var d = r1 - r2;

                //var dt = Math.Abs((dl - d).Value);
                //var da = Math.Abs(((dl + d) / 2).Value);

                //var max = Config.AvgChange * 1M;

                var powerRatio = 0M;
                var powerNote = "";
                decimal usedPower = 0;

                if (PowerSignal != null)
                {
                    var instantPower = PowerSignal.LastSignalResult as PowerSignalResult;
                    var barPower = PowerSignal.Indicator.Results.Last();
                    result.VolumeTime = instantPower != null && instantPower.Value > 0 ? DataTime.Current : DataTime.LastBar;
                    usedPower = result.VolumeTime == DataTime.Current ? instantPower.Value : barPower.Value.Value;
                    powerRatio = (Config.PowerThreshold - usedPower) / 100;
                    powerRatio = powerRatio > 0 ? powerRatio * Config.PowerPositiveMultiplier : powerRatio * Config.PowerNegativeMultiplier;
                    powerNote = $"bar: {barPower.Date} rsiBar: {barPower.Value} rsiInstant: {(instantPower == null ? 0 : instantPower.Value)}";
                    result.VolumePower = usedPower;
                    result.VolumeRatio = powerRatio;
                }

                var dtRatio = 0M;

                //if (dt < max && da < max)
                //{
                //    dtRatio = ((max - dt) / max);
                //}


                var divide = 0;
                if (dtRatio != 0) divide++;
                if (powerRatio != 0) divide++;
                var average = divide > 0 ? (powerRatio + dtRatio) / divide : 0;

                if (Algo.IsMorningStart() && LastCross != 0)
                {
                    var difRatio = (double)(Math.Abs(LastCross) / Config.AvgChange);
                    if (difRatio > 2.0)
                    {
                        average = -(decimal)(1 / (1 + Math.Pow(Math.E, -(0.6 * difRatio))));
                    }
                }

                //Watch("sensitivity/trendRatio", dtRatio);
                Watch("sensitivity/volumePower", usedPower);
                //result.TrendRatio = dtRatio;
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

        protected override void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        {
            base.LoadNewBars(sender, e);
        }

        protected override void OrderCompletedByAlgo(OrderEventArgs e)
        {

            if (Algo.Simulation && !Algo.MultipleTestOptimization && e.Order.SignalResult.Signal == this)
            {
                Chart("Value").Serie("order").SetColor(Color.Chocolate).SetSymbol(ZedGraph.SymbolType.Diamond).Add(e.Order.LastUpdate, 5);
            }
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? DateTime.Now;
            decimal lastAvg = 0M, l1 = 0M, l2 = 0M, mpAverage = 0M;
            var result = new CrossSignalResult(this, t ?? DateTime.Now);

            result.MorningSignal = Algo.IsMorningStart(time);
            if (result.MorningSignal) FillMorningCross(time);



            var mp = Algo.GetMarketPrice(Symbol, t);
            result.MarketPrice = mp;

            if (mp > 0) CollectList.Collect(mp, time);

            if (CollectList.Ready && mp > 0)
            {
                mpAverage = CollectList.LastValue();
                result.AveragePrice = mpAverage;

                result.i1Val = l1 = i1k.NextValue(mpAverage).Value.Value;
                result.i2Val = l2 = i2k.NextValue(mpAverage).Value.Value;

                AnalyseList.Collect(l1 - l2, time);
                crossBars.Push(l1 - l2);

                var cross = result.Cross = Helper.Cross(crossBars.ToArray, 0);
                if (LastCross == 0 && cross != 0)
                {
                    LastCross = cross;
                    Log($"First cross identified: {cross}", LogLevel.Debug, t);
                }
                else if (cross != 0) LastCross = cross;
                result.LastCross = LastCross;

                if (AnalyseList.Count > 0)
                {
                    //var closeAverages = AnalyseList.Averages(Config.Lookback, OHLCType.Close, closeWarmupList);
                    //var closeLast = closeAverages.Last().Close;

                    //var closeRsiList = closeAverages.GetRsi(AnalyseList.BestLookback(closeAverages.Count, Config.Lookback));
                    //var closeRsi = result.Rsi = closeRsiList.Last().Rsi.HasValue ? (decimal)closeRsiList.Last().Rsi.Value : 0;
                    var ohlc = OHLCType.Close; // (closeRsi == 0 || closeRsi == 100) ? OHLCType.Close : (closeRsi > 50 ? OHLCType.High : OHLCType.Low);
                    var rsiEffect = 0; //closeRsi == 0 ? 0 : Math.Abs(50 - closeRsi) / 100;

                    if (Config.Dynamic)
                    {
                        var sensitivity = CalculateSensitivity();
                        applySensitivity(sensitivity);
                        result.Sensitivity = sensitivity;
                    }

                    var totalSize = Math.Max(Convert.ToInt32(Lookback - (rsiEffect) * (Lookback)), 1);
                    lastAvg = result.Dif = AnalyseList.LastValue(Lookback, OHLCType.HL2C4);

                    RsiList.Period = BarPeriod.Sec30;
                    RsiOfRsiList.Period = BarPeriod.Sec30;

                    RsiList.Collect(lastAvg, time);
                    var rsiList = RsiList.RsiList(6);
                    var rsiListLast = rsiList.Last();
                    var rsi = result.Rsi = rsiListLast.Rsi.HasValue ? (decimal)rsiListLast.Rsi.Value : 0;
                    var rsiOfRsi = 0M;

                    if (rsi > 0 && rsi < 100)
                    {
                        RsiOfRsiList.Collect(rsi, time);
                        var rsiOfRsiList = RsiOfRsiList.RsiList(3);
                        var rsiOfRsiListLast = rsiOfRsiList.Last();
                        rsiOfRsi = rsiOfRsiListLast.Rsi.HasValue ? (decimal)rsiOfRsiListLast.Rsi.Value : 0;
                    }

                    var rsiReady = result.RsiReady = rsi > 0 && rsi < 100 && rsiOfRsi > 0 && rsiOfRsi < 100;
                    var down = rsiOfRsi < 50 & rsi < 50;
                    var up = rsiOfRsi > 50 & rsi > 50;

                    var avgChangeL1 = AvgChange;
                    var avgChangeL2 = AvgChange / 4;

                    var topL1 = lastAvg > avgChangeL1/* && (result.MorningSignal || (lastAvg < Config.AvgChange * 2))*/;
                    var belowL1 = lastAvg < -avgChangeL1 /* && (result.MorningSignal || (lastAvg > -Config.AvgChange * 2))*/;

                    var topL2 = lastAvg > avgChangeL2 && lastAvg < avgChangeL2 * 2;
                    var belowL2 = lastAvg < -avgChangeL2 && lastAvg > -avgChangeL2 * 2;

                    if (topL1 && (!rsiReady || up))
                    {
                        result.CrossType = CrossType.AfterUp;
                        result.finalResult = BuySell.Buy;
                    }
                    else if (topL2 && (rsiReady && down))
                    {
                        result.CrossType = CrossType.AfterDown;
                        result.preResult = BuySell.Sell;
                    }
                    else if (belowL1 && (!rsiReady || down))
                    {
                        result.finalResult = BuySell.Sell;
                        result.CrossType = CrossType.BeforeDown;
                    }
                    else if (belowL2 && (rsiReady && up))
                    {
                        result.preResult = BuySell.Buy;
                        result.CrossType = CrossType.BeforeUp;
                    }



                    if (Algo.Simulation && !Algo.MultipleTestOptimization)
                    {
                        Chart("Value").Serie("i1").SetColor(Color.Blue).Add(time, l1);
                        Chart("Value").Serie("Dif").SetColor(Color.Red).Add(time, result.Dif);
                        //Chart("Value").Serie("ohlc").SetColor(Color.Aqua).Add(time, (decimal)ohlc);
                        //if (result.Sensitivity != null)
                        //    Chart("Value").Serie("volume").SetColor(Color.DarkOrange).Add(time, result.Sensitivity.VolumePower * 0.1M);
                        if (i1k.Results.Last().Date.Hour <= time.Hour)
                            Chart("Value").Serie("bar").SetColor(Color.DarkCyan).Add(i1k.Results.Last().Date, i1k.Results.Last().Value.Value);
                        Chart("Value").Serie("rsi").SetColor(Color.Black).Add(time, rsi * 0.1M);
                        Chart("Value").Serie("rsi2").SetColor(Color.Silver).Add(time, rsiOfRsi * 0.1M);
                        //Chart("Value").Serie("rsit").SetColor(Color.DimGray).Add(time, 10);
                        Chart("Value").Serie("rsil").SetColor(Color.Black).Add(time, 5);
                        
                        Chart("Value").Serie("avg").SetColor(Color.DarkOrange).Add(time, avgChangeL1);
                        Chart("Value").Serie("navg").SetColor(Color.DarkOrange).Add(time, -avgChangeL1);

                        Chart("Value").Serie("pre").SetColor(Color.DarkGoldenrod).SetSymbol(result.preResult.HasValue ? ZedGraph.SymbolType.Plus : ZedGraph.SymbolType.None).Add(time, result.preResult.HasValue ? (result.preResult == BuySell.Buy ? 1 : -1) : 0);
                        Chart("Value").Serie("final").SetColor(Color.DarkGreen).SetSymbol(result.finalResult.HasValue ? ZedGraph.SymbolType.HDash : ZedGraph.SymbolType.None).Add(time, result.finalResult.HasValue ? (result.finalResult == BuySell.Buy ? 1 : -1) : 0);


                    }

                    if (time.Hour % 4 == 0 && time.Minute == 1 && time.Second == 1 && Algo.Simulation && !Algo.MultipleTestOptimization)
                    {
                        SaveCharts(time);
                    }
                }
                
            }



            if (time.Second % 10 == 0)
            {
                Log($"Report: lc:{LastCross}, cs:{CollectList.Count}, as:{AnalyseList.Count}, asz:{AnalyseList.List.QueSize} {result}", LogLevel.Verbose, time);
            }

            //if (mp > 0)
            //{
            //    TrackCollectList(time, mp);
            //    TrackAnalyseList(time);
            //}

            return result;
        }


    }
}