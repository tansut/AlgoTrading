// algo
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kalitte.Trading.Algos
{
    public class Bist30 : AlgoBase
    {

        // order params
        [AlgoParam(6.0)]
        public decimal CrossOrderQuantity { get; set; }
        [AlgoParam(3.0)]
        public decimal RsiTrendOrderQuantity { get; set; }

        // cross
        [AlgoParam(true)]
        public bool DynamicCross { get; set; }
        [AlgoParam(45)]
        public decimal CrossRsiMin { get; set; }
        [AlgoParam(55)]
        public decimal CrossRsiMax { get; set; }
        [AlgoParam(88)]
        public decimal PowerCrossThreshold { get; set; }
        [AlgoParam(1.3)]
        public decimal PowerCrossNegativeMultiplier { get; set; }
        [AlgoParam(2.8)]
        public decimal PowerCrossPositiveMultiplier { get; set; }

        // ma cross
        [AlgoParam(5)]
        public int MovPeriod { get; set; }
        [AlgoParam(9)]
        public int MovPeriod2 { get; set; }
        [AlgoParam(0.32)]
        public decimal MaAvgChange { get; set; }


        [AlgoParam(false)]
        public bool SimulateOrderSignal { get; set; }

        // not used
        [AlgoParam(72)]
        public decimal RsiHighLimit { get; set; }
        [AlgoParam(28)]
        public decimal RsiLowLimit { get; set; }
        [AlgoParam(0)]
        public decimal RsiProfitDeltaLowLimit { get; set; }
        [AlgoParam(0)]
        public decimal RsiProfitDeltaHighLimit { get; set; }

        [AlgoParam(2)]
        public decimal RsiTrendThreshold { get; set; }

        [AlgoParam(1)]
        public decimal RsiValueSignalSensitivity { get; set; }


        // rsi profit
        [AlgoParam(0)]
        public decimal RsiProfitInitialQuantity { get; set; }
        [AlgoParam(0)]
        public decimal RsiProfitKeepQuantity { get; set; }
        [AlgoParam(0)]
        public decimal RsiProfitStart { get; set; }
        [AlgoParam(0)]
        public decimal RsiProfitPriceStep { get; set; }

        [AlgoParam(false)]
        public bool RsiProfitEnableLimitingSignalsOnStart { get; set; }

        

        // global profit
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
        [AlgoParam(0)]
        public decimal ProfitStart { get; set; }
        [AlgoParam(false)]
        public bool ProfitUseMonitor { get; set; }

        // fibonachi
        [AlgoParam(0)]
        public decimal PriceLowLimit { get; set; }
        [AlgoParam(0)]
        public decimal PriceHighLimit { get; set; }

        // global loss
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

        // rsi
        [AlgoParam(14)]
        public int Rsi { get; set; }

        // cross macd
        [AlgoParam(0)]
        public int MACDShortPeriod { get; set; }
        [AlgoParam(9)]
        public int MACDLongPeriod { get; set; }
        [AlgoParam(0.05)]
        public decimal MacdAvgChange { get; set; }
        [AlgoParam(9)]
        public int MACDTrigger { get; set; }


        // general
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
        [AlgoParam(0.015)]
        public decimal RsiGradientTolerance { get; set; }
        [AlgoParam(0.005)]
        public decimal RsiGradientLearnRate { get; set; }
        [AlgoParam(1)]
        public decimal RsiGradientSensitivity { get; set; }
        [AlgoParam(0.0015)]
        public decimal ProfitGradientTolerance { get; set; }
        [AlgoParam(0.001)]
        public decimal ProfitGradientLearnRate { get; set; }


        CrossSignal maSignal = null;
        CrossSignal macSignal = null;

        ProfitSignal profitSignal = null;
        LossSignal lossSignal = null;
        ProfitSignal rsiProfitSignal = null;

        //TrendSignal rsiTrendSignal = null;
        IndicatorAnalyser rsiValue = null;
        PowerSignal powerSignal = null;
        ClosePositionsSignal closePositionsSignal = null;
        GradientSignal rsiHigh;
        GradientSignal rsiLow;


        public void InitSignals()
        {
            var periodData = GetSymbolData(this.Symbol, this.SymbolPeriod);

            powerSignal.Indicator = new Rsi(periodData.Periods, PowerLookback, CandlePart.Volume);

            if (maSignal != null)
            {
                maSignal.i1k = new Macd(periodData.Periods, MovPeriod, MovPeriod2, MACDTrigger);
                maSignal.i2k = new Custom((q) => 0, periodData.Periods);
                maSignal.PowerSignal = powerSignal;

                //maSignal.TrackStart = trackStart ?? maSignal.TrackStart;
                //maSignal.TrackEnd = trackFinish?? maSignal.TrackEnd
            }

            if (macSignal != null)
            {
                var macdi = new Macd(periodData.Periods, MACDShortPeriod, MACDLongPeriod, MACDTrigger);
                macSignal.i1k = macdi;
                macSignal.i2k = macdi.Trigger;
            }

            var rsi = new Rsi(periodData.Periods, Rsi);

            rsiValue.i1k = rsi;

            if (rsiLow != null) rsiLow.Indicator = rsi;
            if (rsiHigh != null) rsiHigh.Indicator = rsi;


            Signals.ForEach(p =>
            {
                p.Init();
            });
        }


        public override void InitializeBars(string symbol, BarPeriod period, DateTime? t = null)
        {
            var time = t ?? DateTime.Now;
            //var tm = new DateTime(time.Year, time.Month, time.Day, time.Hour, 59, 0);
            //var minBars = GetPeriodBars(symbol, BarPeriod.Min, tm);
            //this.Symbols.Add(new SymbolData(symbol, minBars));                   
            base.InitializeBars(symbol, period, t);
            InitSignals();
        }

        public ProfitSignal CreateProfitSignal(string name, string symbol)
        {
            var signal = new ProfitSignal(name, symbol, this, this.ProfitStart, this.ProfitInitialQuantity, this.ProfitQuantityStep, this.ProfitQuantityStepMultiplier, ProfitPriceStep, ProfitKeepQuantity);
            signal.GradientTolerance = ProfitGradientTolerance;
            signal.GradientLearnRate = ProfitGradientLearnRate;
            signal.UsePriceMonitor = ProfitUseMonitor;
            return signal;
        }

        public void CreateSignals()
        {
            //DateTime? trackStart = new DateTime(2022, 03, 15, 9, 30, 0);
            //DateTime? trackFinish = new DateTime(2022, 03, 15, 18, 15, 0);

            DateTime? trackStart = null;
            DateTime? trackFinish = null;

            this.powerSignal = new PowerSignal("power", Symbol, this);
            this.Signals.Add(this.powerSignal);

            this.rsiValue = new IndicatorAnalyser("rsi", Symbol, this);
            rsiValue.SignalSensitivity = RsiValueSignalSensitivity;
            this.Signals.Add(this.rsiValue);


            if (RsiTrendOrderQuantity > 0)
            {
                var rsiColSize = Convert.ToInt32(DataCollectSize);
                var rsiAnalSize = Convert.ToInt32(DataAnalysisSize);
                var rsiSignalSensitivity = RsiGradientSensitivity;

                rsiHigh = new GradientSignal("rsi-high", Symbol, this, RsiHighLimit, 100);
                rsiHigh.CollectSize = rsiColSize;
                rsiHigh.AnalyseSize = rsiAnalSize;
                rsiHigh.SignalSensitivity = rsiSignalSensitivity;
                rsiHigh.AnalyseAverage = Average.Ema;
                rsiHigh.Tolerance = RsiGradientTolerance;
                rsiHigh.LearnRate = RsiGradientLearnRate;                

                rsiLow = new GradientSignal("rsi-low", Symbol, this, RsiLowLimit, 0);
                rsiLow.CollectSize = rsiColSize;
                rsiLow.AnalyseSize = rsiAnalSize;
                rsiLow.SignalSensitivity = rsiSignalSensitivity;
                rsiLow.AnalyseAverage = Average.Ema;
                rsiLow.Tolerance = RsiGradientTolerance;
                rsiLow.LearnRate = RsiGradientLearnRate;

                Signals.Add(rsiHigh);
                Signals.Add(rsiLow);
            }


            if (MovPeriod > 0 && CrossOrderQuantity > 0 && !SimulateOrderSignal)
            {
                this.maSignal = new CrossSignal("cross-ma59", Symbol, this) { PowerCrossNegativeMultiplier = PowerCrossNegativeMultiplier, PowerCrossPositiveMultiplier = PowerCrossPositiveMultiplier, PowerCrossThreshold = PowerCrossThreshold, DynamicCross = this.DynamicCross, AvgChange = MaAvgChange };
                this.Signals.Add(maSignal);
            }

            if (MACDShortPeriod > 0 && CrossOrderQuantity > 0 && !SimulateOrderSignal)
            {
                this.macSignal = new CrossSignal("cross-macd593", Symbol, this) { PowerCrossNegativeMultiplier = PowerCrossNegativeMultiplier, PowerCrossPositiveMultiplier = PowerCrossPositiveMultiplier, PowerCrossThreshold = PowerCrossThreshold, DynamicCross = this.DynamicCross, AvgChange = MacdAvgChange };
                this.Signals.Add(macSignal);
            }

            if (SimulateOrderSignal) this.Signals.Add(new FlipFlopSignal("flipflop", Symbol, this, BuySell.Buy));


            if (!SimulateOrderSignal && (this.ProfitInitialQuantity > 0))
            {
                this.profitSignal = CreateProfitSignal("profit", Symbol);
                //this.profitSignal.LimitingSignalTypes.Add(typeof(CrossSignal));
                this.Signals.Add(profitSignal);
            }

            if (!SimulateOrderSignal && (this.RsiProfitInitialQuantity > 0))
            {
                this.rsiProfitSignal = CreateProfitSignal("rsi-profit", Symbol);
                this.rsiProfitSignal.InitialQuantity = RsiProfitInitialQuantity;
                this.rsiProfitSignal.KeepQuantity = RsiProfitKeepQuantity == 0 ? ProfitKeepQuantity : RsiProfitKeepQuantity;
                this.rsiProfitSignal.PriceChange = RsiProfitStart == 0 ? ProfitStart : RsiProfitStart;
                this.rsiProfitSignal.PriceStep = RsiProfitPriceStep == 0 ? ProfitPriceStep: RsiProfitPriceStep;
                this.rsiProfitSignal.LimitingSignalTypes.Add(typeof(GradientSignal));
                this.rsiProfitSignal.EnableLimitingSignalsOnStart = RsiProfitEnableLimitingSignalsOnStart;
                this.Signals.Add(rsiProfitSignal);
            }

            if (!SimulateOrderSignal && (this.LossInitialQuantity > 0))
            {
                this.lossSignal = new LossSignal("loss", Symbol, this, this.LossStart, this.LossInitialQuantity, this.LossQuantityStep, this.LossQuantityStepMultiplier, LossPriceStep, this.LossKeepQuantity);
                this.lossSignal.LimitingSignalTypes.Add(typeof(GradientSignal));
                if (rsiLow != null) this.lossSignal.CostSignals.Add(rsiLow);
                if (rsiHigh != null) this.lossSignal.CostSignals.Add(rsiHigh);
                this.Signals.Add(lossSignal);
            }

            closePositionsSignal = new ClosePositionsSignal("daily-close", Symbol, this, ClosePositionsDaily);
            if (ClosePositionsDaily) Signals.Add(closePositionsSignal);

            Fibonacci fib = this.PriceLowLimit > 0 ? new Fibonacci(PriceLowLimit, PriceHighLimit) : null;

            Signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
                p.PerfMon = this.Watch;

                var analyser = p as AnalyserBase;
                var profit = p as ProfitLossSignal;

                if (analyser != null)
                {
                    analyser.CollectSize = DataCollectSize;
                    analyser.AnalyseAverage = Average.Ema;
                    analyser.AnalyseSize = Convert.ToInt32(DataAnalysisSize);
                    analyser.CollectAverage = DataCollectUseSma ? Average.Sma : Average.Ema;
                    analyser.AnalyseAverage = DataAnalysisUseSma ? Average.Sma : Average.Ema;
                    analyser.TrackStart = trackStart ?? analyser.TrackStart;
                    analyser.TrackEnd = trackFinish ?? analyser.TrackEnd;
                }

                if (profit != null)
                {
                    profit.FibonacciLevels = fib;                    
                }
            });

            if (rsiHigh != null && rsiLow != null)
            {
                rsiLow.AnalyseAverage = Average.Ema;
                rsiHigh.AnalyseAverage = Average.Ema;
            }
        }


        public override void ClosePositions(string symbol, SignalResult signalResult)
        {
            if (signalResult == null) signalResult = new SignalResult(closePositionsSignal, Now);
            var time = signalResult.SignalTime;
            foreach (var item in UserPortfolioList)
            {
                if (!item.Value.IsEmpty)
                {
                    Log($"Closing positions for {symbol} at {time}", LogLevel.Info, time);
                    sendOrder(symbol, item.Value.Quantity, item.Value.Side == BuySell.Buy ? BuySell.Sell : BuySell.Buy, "close position", 0, OrderIcon.PositionClose, time, signalResult, true);
                }
            }
        }



        public override void ConfigureMonitor()
        {

            if (maSignal != null)
            {
                this.Watch.AddFilter($"{maSignal.Name}/sensitivity", 10);
            }
            if (powerSignal != null)
            {
                this.Watch.AddFilter($"{powerSignal.Name}/volume", 10);
                this.Watch.AddFilter($"{powerSignal.Name}/VolumePerSecond", 10);
            }
            //if (rsiTrendSignal != null)
            //{
            //    this.Watch.AddFilter($"{rsiTrendSignal.Name}/value", 5);
            //    this.Watch.AddFilter($"{rsiTrendSignal.Name}/speed", 10);
            //}
            base.ConfigureMonitor();
        }

        public override void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            CreateSignals();
            base.Init();
        }


        private void HandleProfitLossSignal(ProfitLossSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (portfolio.IsEmpty) return;
            if (result.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (result.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var lastSignalTime = portfolio.LastPositionOrder == null ? DateTime.MinValue : portfolio.LastPositionOrder.SignalResult.SignalTime;
            if (signal.SignalType == ProfitOrLoss.Loss && (result.SignalTime - lastSignalTime).TotalSeconds < 60)
            {
                Log($"{signal.Name} {result} received but there is no time dif between {lastSignalTime} and {result.SignalTime}", LogLevel.Warning);
            };



            decimal keep = result.KeepQuantity; // portfolio.IsLastOrderInstanceOf(typeof(TrendSignal)) ? 0: result.KeepQuantity;
            decimal quantity = result.Quantity;
            decimal remaining = portfolio.Quantity - quantity;
            quantity = Math.Min(portfolio.Quantity, remaining >= signal.KeepQuantity ? quantity : portfolio.Quantity - keep);

            if (quantity > 0)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, OriginalPrice: {result.OriginalPrice} MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Info, result.SignalTime);
                sendOrder(Symbol, quantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? OrderIcon.TakeProfit : OrderIcon.StopLoss, result.SignalTime, result);
            }
        }





        public override void Decide(Signal signal, SignalEventArgs data)
        {
            if (data.Result.SignalTime.Second % 30 == 0 && !Simulation)
            {
                Log($"Process [{data.Result.Signal.Name}] using {data.Result} from ", LogLevel.Debug);
            }
            if (!WaitForOrder("Decide")) return;
            
            var result = data.Result;

            Log($"Starting to process signal as {data.Result} from {data.Result.Signal.Name}", LogLevel.Verbose);
            if (result.Signal is ProfitLossSignal)
            {
                HandleProfitLossSignal((ProfitLossSignal)result.Signal, (ProfitLossResult)result);
            }
            else if ((result.Signal.Name == "rsi-high") || (result.Signal.Name == "rsi-low"))
            {
                HandleRsiLimitSignal((GradientSignal)result.Signal, (GradientSignalResult)result);
            }
            else if (result.Signal is CrossSignal)
            {
                HandleCrossSignal((CrossSignal)result.Signal, (CrossSignalResult)result);
            }
            else if (result.Signal.Name == "daily-close")
            {
                HandleDailyCloseSignal((ClosePositionsSignal)result.Signal, result);
            }
        }

        private void HandleRsiLimitSignal(GradientSignal signal, GradientSignalResult signalResult)
        {
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);
            var lastOrder = portfolio.CompletedOrders.LastOrDefault();
            var delta = signalResult.finalResult == BuySell.Buy ? RsiProfitDeltaLowLimit : RsiProfitDeltaHighLimit;
            
            var rsiOrders = portfolio.GetLastPositionOrders(typeof(GradientSignal));
            var lastOrderIsLoss = portfolio.LastOrderIsLoss; 
            if (lastOrderIsLoss && rsiOrders.Count > 0) return;
            var keepPosition =  delta == 0 ? rsiOrders.Count > 0 : rsiOrders.Count > 1;

            Log($"HandleRsiLimit: {signalResult.finalResult} {portfolio.IsLong} {portfolio.IsShort} {keepPosition} {signalResult.Gradient.UsedValue}", LogLevel.Verbose);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong && keepPosition) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort && keepPosition) return;

            
            var usedRsiQuantity = RsiTrendOrderQuantity; 

            if (delta > 0)
            {
                if (signalResult.finalResult == BuySell.Buy && signalResult.Gradient.UsedValue > signal.L1 - delta) usedRsiQuantity = usedRsiQuantity / 2;
                else if (signalResult.finalResult == BuySell.Sell && signalResult.Gradient.UsedValue < signal.L1 + delta) usedRsiQuantity = usedRsiQuantity / 2;
            }

            var orderQuantity = portfolio.Quantity + usedRsiQuantity;

            if (!keepPosition && !portfolio.IsEmpty && portfolio.Side == signalResult.finalResult)
            {
                orderQuantity = usedRsiQuantity - portfolio.Quantity;
            }

            if (orderQuantity > 0)
            {
                sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"{signal.Name}[{signalResult}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            }
        }

        public void HandleDailyCloseSignal(ClosePositionsSignal signal, SignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            if (!portfolio.IsEmpty) ClosePositions(Symbol, result);
        }

        public void HandleCrossSignal(CrossSignal signal, CrossSignalResult signalResult)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);            
            var keepPosition = portfolio.IsLastPositionOrderInstanceOf(typeof(CrossSignal)); // lastOrder != null && lastOrder.SignalResult.Signal.GetType().IsAssignableFrom(typeof(CrossSignal));

            Log($"HandleCross: {signalResult.finalResult} {portfolio.IsLong} {portfolio.IsShort} {keepPosition}", LogLevel.Verbose);
            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong && keepPosition) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort && keepPosition) return;

            var orderQuantity = portfolio.Quantity + CrossOrderQuantity;

            if (!keepPosition && !portfolio.IsEmpty && portfolio.Side == signalResult.finalResult)
            {
                orderQuantity = CrossOrderQuantity - portfolio.Quantity;
            }


            if (orderQuantity > 0)
            {
                var cross = (CrossSignal)signalResult.Signal;
                var currentRsi = rsiValue.GetCurrentValue(); 

                if (currentRsi.HasValue)
                {
                    if (!signalResult.MorningSignal && CrossRsiMax != 0 && signalResult.finalResult == BuySell.Buy && currentRsi != 0 && currentRsi > CrossRsiMax)
                    {
                        Log($"Ignoring cross {signalResult.finalResult} signal since currentRsi is {currentRsi}", LogLevel.Debug);
                        return;
                    };
                    if (!signalResult.MorningSignal && CrossRsiMin != 0 && signalResult.finalResult == BuySell.Sell && currentRsi != 0 && currentRsi < CrossRsiMin)
                    {
                        Log($"Ignoring cross {signalResult.finalResult} signal since currentRsi is {currentRsi}", LogLevel.Debug);
                        return;
                    };
                }

                sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}/{cross.AvgChange.ToCurrency()},{cross.AnalyseSize}, {(currentRsi.HasValue ? currentRsi.Value.ToCurrency():0)}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            }
        }

        public override void CompletedOrder(ExchangeOrder order)
        {

            var signal = order.SignalResult.Signal;

            var tp = signal as ProfitLossSignal;
            var cross = order.SignalResult as CrossSignalResult;
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);

            signal.AddOrder(1, order.Quantity);

            if (tp != null)
            {
                tp.IncrementParams();
            }
            else
            {
                Signals.Where(p => p is ProfitLossSignal).Select(p => (ProfitLossSignal)p).ToList().ForEach(p => p.ResetOrders());
            }
            if (portfolio.IsEmpty)  
            {
                if (portfolio.LastOrderIsLoss && portfolio.IsLastPositionOrderInstanceOf(typeof(GradientSignal))) {
                    Log($"Skipping cross reset since last order is position close by loss/rsi", LogLevel.Debug);
                }
                else Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.ResetCross());
            }
            base.CompletedOrder(order);
        }




        public Bist30() : base()
        {

        }


        public Bist30(Dictionary<string, object> init) : base(init)
        {

        }

        public override string ToString()
        {
            var assembly = typeof(Bist30).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }



    }
}
