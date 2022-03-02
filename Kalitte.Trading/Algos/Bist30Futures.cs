﻿// algo
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kalitte.Trading.Algos
{
    public class Bist30Futures : AlgoBase
    {

        public PowerSignalResult LastPower { get; set; } = null;

        [AlgoParam(6.0)]
        public decimal CrossOrderQuantity { get; set; }

        [AlgoParam(3.0)]
        public decimal RsiTrendOrderQuantity { get; set; }


        [AlgoParam(5)]
        public int MovPeriod { get; set; }

        [AlgoParam(9)]
        public int MovPeriod2 { get; set; }

        [AlgoParam(true)]
        public bool DynamicCross { get; set; }


        [AlgoParam(0.32)]
        public decimal MaAvgChange { get; set; }

        [AlgoParam(false)]
        public bool SimulateOrderSignal { get; set; }


        [AlgoParam(60)]
        public decimal RsiHighLimit { get; set; }

        [AlgoParam(40)]
        public decimal RsiLowLimit { get; set; }

        [AlgoParam(2)]
        public decimal RsiTrendThreshold { get; set; }


        [AlgoParam(1)]
        public decimal RsiTrendSensitivity { get; set; }

        [AlgoParam(1)]
        public decimal PriceTrendSensitivity { get; set; }


        [AlgoParam(1)]
        public decimal RsiProfitInitialQuantity { get; set; }

        [AlgoParam(0)]
        public decimal RsiProfitStep { get; set; }

        [AlgoParam(1)]
        public decimal RsiProfitQuantityStep { get; set; }


        [AlgoParam(0)]
        public decimal RsiProfitQuantityStepMultiplier { get; set; }

        [AlgoParam(0)]
        public decimal RsiProfitKeepQuantity { get; set; }


        [AlgoParam(20)]
        public decimal RsiProfitStart { get; set; }


        [AlgoParam(0)]
        public decimal ProfitInitialQuantity { get; set; }

        [AlgoParam(0)]
        public decimal ProfitPriceStep { get; set; }

        [AlgoParam(1)]
        public decimal ProfitQuantityStep { get; set; }
        

        [AlgoParam(0)]
        public decimal ProfitQuantityStepMultiplier { get; set; }

        [AlgoParam(0)]
        public decimal ProfitKeepQuantity { get; set; }


        [AlgoParam(20)]
        public decimal ProfitStart { get; set; }

        [AlgoParam(0)]
        public decimal LossPriceStep { get; set; }

        [AlgoParam(0)]
        public decimal LossQuantityStepMultiplier { get; set; }

        [AlgoParam(1)]
        public decimal LossQuantityStep { get; set; }

        [AlgoParam(0)]
        public decimal LossInitialQuantity { get; set; }



        [AlgoParam(0)]
        public decimal LossKeepQuantity { get; set; }


        [AlgoParam(4)]
        public decimal LossStart { get; set; }


        [AlgoParam(14)]
        public int Rsi { get; set; }

        [AlgoParam(0)]
        public int MACDShortPeriod { get; set; }

        [AlgoParam(9)]
        public int MACDLongPeriod { get; set; }

        [AlgoParam(0.05)]
        public decimal MacdAvgChange { get; set; }

        [AlgoParam(9)]
        public int MACDTrigger { get; set; }


        [AlgoParam(5)]
        public int PowerLookback { get; set; }


        [AlgoParam(12)]
        public int DataCollectSize { get; set; }

        [AlgoParam(48)]
        public int DataAnalysisSize { get; set; }

        [AlgoParam(true)]
        public bool DataCollectUseSma { get; set; }

        [AlgoParam(false)]
        public bool DataAnalysisUseSma { get; set; }

        [AlgoParam(88)]
        public decimal PowerCrossThreshold { get; set; }

        [AlgoParam(1.3)]
        public decimal PowerCrossNegativeMultiplier { get; set; }

        [AlgoParam(2.8)]
        public decimal PowerCrossPositiveMultiplier { get; set; }

        public FinanceBars MinBars = null;

        CrossSignal maSignal = null;
        CrossSignal macSignal = null;
        //TakeProfitOrLossSignal takeProfitSignal = null;
        ProfitSignal profitSignal = null;
        LossSignal lossSignal = null;
        //TakeProfitOrLossSignal lossSignal = null;
        TrendSignal rsiTrendSignal = null;
        TrendProfitSignal rsiProfitSignal = null;
        TrendSignal maTrendSignal = null;
        TrendSignal priceTrend = null;
        TrendSignal atrTrend = null;
        PowerSignal powerSignal = null;



        public override void InitMySignals(DateTime t)
        {

            var periodData = GetSymbolData(this.Symbol, this.SymbolPeriod);
            //var oneMinData = GetSymbolData(this.Symbol, BarPeriod.Min);
            var price = new Price(periodData.Periods, PowerLookback);
            priceTrend.i1k = price;

            var atr = new Atrp(periodData.Periods, PowerLookback);
            atrTrend.i1k = atr;

            powerSignal.Indicator = new Rsi(periodData.Periods, PowerLookback, CandlePart.Volume);

            if (maSignal != null)
            {
                maSignal.i1k = new Macd(periodData.Periods, MovPeriod, MovPeriod2, MACDTrigger);
                maSignal.i2k = new Custom((q) => 0, periodData.Periods, MovPeriod + MovPeriod2 + MACDTrigger);
                maSignal.PowerSignal = powerSignal;

                if (maTrendSignal != null)
                {
                    maTrendSignal.i1k = maSignal.i1k;
                }
            }

            if (macSignal != null)
            {
                var macdi = new Macd(periodData.Periods, MACDShortPeriod, MACDLongPeriod, MACDTrigger);
                macSignal.i1k = macdi;
                macSignal.i2k = macdi.Trigger;
            }

            if (rsiTrendSignal != null)
            {
                var rsi = new Rsi(periodData.Periods, Rsi);
                rsiTrendSignal.i1k = rsi;
            }



            Signals.ForEach(p =>
            {
                p.Init();
            });
        }


        public override void InitializeBars(string symbol, BarPeriod period, DateTime? t = null)
        {
            //this.MinBars = GetPeriodBars(symbol, period, t);
            //this.oneMinBars = GetPeriodBars(symbol, BarPeriod.Min, t);
            //base.InitializeBars(symbol, BarPeriod.Min, t);
            base.InitializeBars(symbol, period, t);
        }

        public void InitSignals()
        {
            this.priceTrend = new TrendSignal("price-trend", Symbol, this);
            priceTrend.ReferenceType = TrendReference.LastCheck;
            priceTrend.SignalSensitivity = PriceTrendSensitivity;

            this.atrTrend = new TrendSignal("atr-trend", Symbol, this);
            atrTrend.HowToReset = ResetList.Always;

            this.powerSignal = new PowerSignal("power", Symbol, this);

            //this.Signals.Add(this.priceTrend);
            //this.Signals.Add(this.atrTrend);
            this.Signals.Add(this.powerSignal);


            if (MovPeriod > 0 && !SimulateOrderSignal)
            {
                this.maSignal = new CrossSignal("cross:ma59", Symbol, this) { PowerCrossNegativeMultiplier = PowerCrossNegativeMultiplier, PowerCrossPositiveMultiplier = PowerCrossPositiveMultiplier, PowerCrossThreshold = PowerCrossThreshold, DynamicCross = this.DynamicCross, AvgChange = MaAvgChange };
                this.Signals.Add(maSignal);
                this.maTrendSignal = new TrendSignal("ma-trend", Symbol, this);
                Signals.Add(maTrendSignal);
            }

            if (MACDShortPeriod > 0 && !SimulateOrderSignal)
            {
                this.macSignal = new CrossSignal("cross:macd593", Symbol, this) { PowerCrossNegativeMultiplier = PowerCrossNegativeMultiplier, PowerCrossPositiveMultiplier = PowerCrossPositiveMultiplier, PowerCrossThreshold = PowerCrossThreshold, DynamicCross = this.DynamicCross, AvgChange = MacdAvgChange };
                this.Signals.Add(macSignal);
            }

            if (SimulateOrderSignal) this.Signals.Add(new FlipFlopSignal("flipflop", Symbol, this, BuySell.Buy));


            if (!SimulateOrderSignal && (this.ProfitInitialQuantity > 0))
            {
                this.profitSignal = new ProfitSignal("profit", Symbol, this, this.ProfitStart, this.ProfitInitialQuantity, this.ProfitQuantityStep, this.ProfitQuantityStepMultiplier, ProfitPriceStep);                
                this.Signals.Add(profitSignal);
            }

            if (!SimulateOrderSignal && (this.LossInitialQuantity > 0))
            {
                this.lossSignal = new LossSignal("loss", Symbol, this, this.LossStart, this.LossInitialQuantity, this.LossQuantityStep, this.LossQuantityStepMultiplier, LossPriceStep);                
                this.Signals.Add(lossSignal);
            }
            if (!SimulateOrderSignal && (RsiHighLimit > 0 || RsiLowLimit > 0))
            {
                rsiTrendSignal = new TrendSignal("rsi-trend", Symbol, this, RsiLowLimit == 0 ? new decimal?() : RsiLowLimit, RsiHighLimit == 0 ? new decimal() : RsiHighLimit) { };
                rsiTrendSignal.SignalSensitivity = RsiTrendSensitivity;

                if (RsiProfitInitialQuantity > 0)
                {
                    this.rsiProfitSignal = new TrendProfitSignal("rsi-profit", Symbol, this, rsiTrendSignal, RsiTrendThreshold, RsiProfitStart, RsiProfitInitialQuantity, RsiProfitQuantityStep, RsiProfitQuantityStepMultiplier, RsiProfitStep);
                    Signals.Add(rsiProfitSignal);
                }
                this.Signals.Add(rsiTrendSignal);
            }

            Signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
                p.PerfMon = this.Monitor;

                var analyser = p as AnalyserBase;

                if (analyser != null)
                {
                    analyser.CollectSize = DataCollectSize;
                    analyser.AnalyseSize = DataAnalysisSize;
                    analyser.CollectAverage = DataCollectUseSma ? Average.Sma : Average.Ema;
                    analyser.AnalyseAverage = DataAnalysisUseSma ? Average.Sma : Average.Ema;
                }
            });
        }


        public override void ConfigureMonitor()
        {

            if (maSignal != null)
            {
                this.Monitor.AddFilter($"{maSignal.Name}/sensitivity", 10);
            }
            if (powerSignal != null)
            {
                this.Monitor.AddFilter($"{powerSignal.Name}/volume", 10);
                this.Monitor.AddFilter($"{powerSignal.Name}/VolumePerSecond", 10);
            }
            if (rsiTrendSignal != null)
            {
                this.Monitor.AddFilter($"{rsiTrendSignal.Name}/value", 10);
                this.Monitor.AddFilter($"{rsiTrendSignal.Name}/speed", 10);
            }
            if (priceTrend != null)
            {
                this.Monitor.AddFilter($"{priceTrend.Name}/value", 0.5M);
            }
            base.ConfigureMonitor();
        }

        public override void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            //this.AddSymbol(this.Symbol)
            InitSignals();

            base.Init();
        }



        private void HandleProfitLossSignal(ProfitLossSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (portfolio.IsEmpty) return;
            if (result.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (result.finalResult == BuySell.Sell && portfolio.IsShort) return;

            decimal keep = result.Direction == ProfitOrLoss.Profit ? ProfitKeepQuantity : LossKeepQuantity;
            decimal quantity = result.Quantity;
            decimal remaining = portfolio.Quantity - quantity;
            quantity = Math.Min(portfolio.Quantity, remaining >= keep ? quantity : portfolio.Quantity - keep);
            
            if (quantity > 0)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Debug, result.SignalTime);
                sendOrder(Symbol, quantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? OrderIcon.TakeProfit : OrderIcon.StopLoss, result.SignalTime, result);
            }
        }


        

        private void HandleMaTrendSignal(TrendSignal signal, TrendSignalResult result)
        {
                       
        }

        private void HandlePriceTrendSignal(TrendSignal signal, TrendSignalResult result)
        {
            //Log($"[price-trend]: {result.Trend.Direction} {result.Trend.NewValue} ", LogLevel.Critical, result.SignalTime);
        }


        public VolatileRatio EstimateVolatility(decimal val)
        {
            if (val < 0.125M) return VolatileRatio.Low;
            else if (val < 0.25M) return VolatileRatio.BelowAverage;
            else if (val < 0.50M) return VolatileRatio.Average;
            else if (val < 0.75M) return VolatileRatio.High;
            else return VolatileRatio.Critical;
        }

        void HandlePowerSignal(PowerSignal signal, PowerSignalResult result)
        {
            return;
            if (LastPower == null && result.Power != PowerRatio.Unknown)
            {
                LastPower = result;
            }

            if (LastPower != null && DynamicCross && Now.Second % 30 == 0)
            {
                //Log($"Current ATR volatility level: {result}", LogLevel.Warning);
                var atrInd = (Atrp)(atrTrend as TrendSignal).i1k;
                var last = atrInd.ResultList.Last;
                Log($"ATR: atr: {last.Atr} tr: {last.Tr} p: {last.Atrp}", LogLevel.Debug);
                Log($"POWER {LastPower.Power}: {LastPower} ", LogLevel.Debug, result.SignalTime);
            }

            if (DynamicCross && result.Power != PowerRatio.Unknown)
            {
                double ratio = 0;
                //if (result.Value < PowerCrossThreshold || PowerCrossThreshold == 0)
                //{
                //    ratio = 0.25 * (double)(100 - result.Value) / 100D;
                //}
                //if (ratio < 0) ratio = 0;
                //ratio = (double)(100 - result.Value) / 100D;
                //Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.AdjustSensitivity(ratio, $"{result.Power}/{result.Value}"));
            }

            //if (DynamicCross)
            //{
            //    var ratio = (double)(100 - result.Value) / 100;
            //    if (result.Value < PowerCrossThreshold)
            //    {
            //        Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.AdjustSensitivity(ratio, $"{result.Power}/{result.Value}"));
            //    }
            //}

            //Log($"{result}", LogLevel.Critical, result.SignalTime);            
        }


        private void HandleAtrTrendSignal(TrendSignal signal, TrendSignalResult result)
        {
            return;
            if (DynamicCross)
            {
                var newVal = EstimateVolatility(result.Trend.NewValue);
                if (VolatileRatio != newVal)
                {
                    VolatileRatio = newVal;
                    var val = result.Trend.NewValue;
                    double ratio = 0;
                    switch (VolatileRatio)
                    {
                        case VolatileRatio.Low:
                            {
                                ratio = 0.20;
                                break;
                            }
                        case VolatileRatio.BelowAverage:
                            {
                                ratio = 0.15;
                                break;
                            }
                        case VolatileRatio.High:
                            {
                                ratio = -0.15;
                                break;
                            }
                        case VolatileRatio.Critical:
                            {
                                ratio = -0.30;
                                break;
                            }
                    }
                    //Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.AdjustSensitivity(ratio, $"{VolatileRatio}({result.Trend.NewValue.ToCurrency()})"));


                }
                if (Now.Second % 10 == 10)
                {
                    Log($"Current ATR volatility level: {result}", LogLevel.Warning);
                    var atrInd = (Atrp)(result.Signal as TrendSignal).i1k;
                    var last = atrInd.ResultList.Last;
                    Log($"ATR last: atr: {last.Atr} tr: {last.Tr} p: {last.Atrp}", LogLevel.Warning);
                    //Log($"Current ATR volatility level: {VolatileRatio}. Adjusted cross to {maSignal.AvgChange}, {maSignal.Periods}. Signal: {result}", LogLevel.Critical, result.SignalTime);
                }
            }
        }


        public override void Decide(Signal signal, SignalEventArgs data)
        {
            var result = data.Result;
            var waitOthers = waitForOperationAndOrders("Decide");
            if (!waitOthers) return;
            operationWait.Reset();
            try
            {
                //Log($"Processing signal as {result.finalResult} from {result.Signal.Name}", LogLevel.Debug);
                if (result.Signal is ProfitLossSignal)
                {
                    var tpSignal = (ProfitLossSignal)(result.Signal);
                    var signalResult = (ProfitLossResult)result;
                    HandleProfitLossSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "rsi-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    if (rsiProfitSignal != null)
                    {                         
                        var profitResult = rsiProfitSignal.HandleTrendSignal(tpSignal, signalResult);
                        if (profitResult != null) HandleProfitLossSignal(rsiProfitSignal, profitResult);
                        else HandleRsiTrendSignal(tpSignal, signalResult);
                    }
                }
                else if (result.Signal.Name == "price-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandlePriceTrendSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "ma-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandleMaTrendSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "atr-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandleAtrTrendSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "power")
                {
                    var tpSignal = (PowerSignal)(result.Signal);
                    var signalResult = (PowerSignalResult)result;
                    HandlePowerSignal(tpSignal, signalResult);
                }
                else if (result.Signal is CrossSignal)
                {
                    var tpSignal = (CrossSignal)(result.Signal);
                    var signalResult = (CrossSignalResult)result;
                    HandleCrossSignal(tpSignal, signalResult);
                }
            }
            finally
            {
                operationWait.Set();
            }
        }

        public void HandleRsiTrendSignal(TrendSignal signal, TrendSignalResult signalResult)
        {
            if (RsiTrendOrderQuantity == 0) return;
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            if (!portfolio.IsEmpty) return;

            var trend = signalResult.Trend;
            BuySell? bs = null;
            if (Math.Abs(trend.Change) < RsiTrendThreshold) return;
            else if (trend.Direction == TrendDirection.ReturnDown || trend.Direction == TrendDirection.MoreUp)
            {
                bs = BuySell.Sell;
            }
            else if (trend.Direction == TrendDirection.ReturnUp || trend.Direction == TrendDirection.LessDown) 
            {
                bs = BuySell.Buy;
            }
            else return;
            sendOrder(Symbol, RsiTrendOrderQuantity, bs.Value, $"[{signalResult.Signal.Name}/{trend.Direction},{trend.Change}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
        }

        public void HandleCrossSignal(CrossSignal signal, CrossSignalResult signalResult)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var orderQuantity = portfolio.Quantity + CrossOrderQuantity;

            var cross = (CrossSignal)signalResult.Signal;
            sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}/{cross.AvgChange},{cross.AnalyseSize}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);


        }

        public override void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            var tp = this.positionRequest.SignalResult as ProfitLossResult;
            var cross = this.positionRequest.SignalResult as CrossSignalResult;
            if (tp != null)
            {
                var tps = ((ProfitLossSignal)tp.Signal);
                tps.IncrementSignal(1, positionRequest.Quantity);
                tps.IncrementParams();
            }
            if (cross != null)
            {
                Signals.Where(p => p is ProfitLossSignal).Select(p => (ProfitLossSignal)p).ToList().ForEach(p => p.ResetChanges());
            }
            base.FillCurrentOrder(filledUnitPrice, filledQuantity);
        }

        public override void sendOrder(string symbol, decimal quantity, BuySell side, string comment = "", decimal lprice = 0, OrderIcon icon = OrderIcon.None, DateTime? t = null, SignalResult signalResult = null, bool disableDelay = false)
        {
            base.sendOrder(symbol, quantity, side, comment, lprice, icon, t, signalResult, disableDelay);
            //if (signalResult is CrossSignalResult)
            //{
            //    var cross = signalResult as CrossSignalResult;
            //    Log($"Cross: {cross}", LogLevel.Order);
            //    Log($"Sensitivity: {cross.Sensitivity}", LogLevel.Order);
            //}

            //Log($"Power was during order: {LastPower}", LogLevel.Order);
        }

        public Bist30Futures() : base()
        {

        }

        //public Bist30Futures(string configFile): base(configFile)
        //{

        //}

        public Bist30Futures(Dictionary<string, object> init) : base(init)
        {

        }

        public override string ToString()
        {
            var assembly = typeof(Bist30Futures).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }



    }
}
