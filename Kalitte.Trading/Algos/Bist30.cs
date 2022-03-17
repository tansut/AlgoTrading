﻿// algo
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kalitte.Trading.Algos
{

    public enum RsiPositionAction
    {
        OpenIfEmpty,
        BuyAdditional,
        ChangePosition
    }

    public class RsiOrderConfig
    {
        [AlgoParam(0)]
        public decimal Quantity { get; set; }
        [AlgoParam(RsiPositionAction.OpenIfEmpty)]
        public RsiPositionAction Action { get; set; }
    }


    public class Bist30 : AlgoBase
    {

        // order params
        [AlgoParam(6.0)]
        public decimal CrossOrderQuantity { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderL1 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderL2 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderL3 { get; set; }


        // ma cross
        [AlgoParam(5)]
        public int MovPeriod { get; set; }
        [AlgoParam(9)]
        public int MovPeriod2 { get; set; }

        // cross rsi
        [AlgoParam(45)]
        public decimal CrossRsiMin { get; set; }
        [AlgoParam(55)]
        public decimal CrossRsiMax { get; set; }

        // fibonachi
        [AlgoParam(0)]
        public decimal PriceLowLimit { get; set; }
        [AlgoParam(0)]
        public decimal PriceHighLimit { get; set; }


        [AlgoParam(null, "RsiLoss")]
        public PLSignalConfig RsiLossConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "RsiProfit")]
        public PLSignalConfig RsiProfitConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "Profit")]
        public PLSignalConfig ProfitConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "RsiHighL1")]
        public GradientSignalConfig RsiHighL1Config { get; set; } = new GradientSignalConfig();

        [AlgoParam(null, "RsiHighL2")]
        public GradientSignalConfig RsiHighL2Config { get; set; } = new GradientSignalConfig();

        [AlgoParam(null, "RsiHighL3")]
        public GradientSignalConfig RsiHighL3Config { get; set; } = new GradientSignalConfig();


        [AlgoParam(null, "RsiLowL1")]
        public GradientSignalConfig RsiLowL1Config { get; set; } = new GradientSignalConfig();

        [AlgoParam(null, "RsiLowL2")]
        public GradientSignalConfig RsiLowL2Config { get; set; } = new GradientSignalConfig();

        [AlgoParam(null, "RsiLowL3")]
        public GradientSignalConfig RsiLowL3Config { get; set; } = new GradientSignalConfig();


        [AlgoParam(null, "MaCross")]
        public CrossSignalConfig MaCrossConfig { get; set; } = new CrossSignalConfig();

        [AlgoParam(null, "VolumePower")]
        public PowerSignalConfig VolumePowerConfig { get; set; } = new PowerSignalConfig();

        [AlgoParam(null, "RsiValue")]
        public AnalyserConfig RsiValueConfig { get; set; } = new AnalyserConfig();

        [AlgoParam(null, "DailyClose")]
        public ClosePositionsSignalConfig DailyCloseConfig { get; set; }

        // rsi
        [AlgoParam(14)]
        public int Rsi { get; set; }

        // general
        [AlgoParam(5)]
        public int PowerLookback { get; set; }
        
        [AlgoParam(12)]
        public int DataCollectSize { get; set; }
        [AlgoParam(48)]
        public int DataAnalysisSize { get; set; }
        [AlgoParam(Average.Ema)]
        public Average DataCollectAverage { get; set; }
        [AlgoParam(Average.Sma)]
        public Average DataAnalysisAverage { get; set; }


        [AlgoParam(0.015)]
        public decimal RsiGradientTolerance { get; set; }
        [AlgoParam(0.005)]
        public decimal RsiGradientLearnRate { get; set; }




        CrossSignal maCross = null;

        ProfitSignal profitSignal = null;
        LossSignal rsiLossSignal = null;
        ProfitSignal rsiProfitSignal = null;

        IndicatorAnalyser rsiValue = null;
        PowerSignal powerSignal = null;
        ClosePositionsSignal closePositionsSignal = null;

        GradientSignal rsiHighL1;
        GradientSignal rsiHighL2;
        GradientSignal rsiHighL3;        

        GradientSignal rsiLowL1;
        GradientSignal rsiLowL2;
        GradientSignal rsiLowL3;


        public void InitSignals()
        {
            var periodData = GetSymbolData(this.Symbol, this.SymbolPeriod);

            powerSignal.Indicator = new Rsi(periodData.Periods, PowerLookback, CandlePart.Volume);
            maCross.i1k = new Macd(periodData.Periods, MovPeriod, MovPeriod2, 9);
            maCross.i2k = new Custom((q) => 0, periodData.Periods);
            maCross.PowerSignal = powerSignal;

            var rsi = new Rsi(periodData.Periods, Rsi);

            rsiValue.i1k = rsi;

            rsiHighL1.Indicator = rsi;
            rsiHighL2.Indicator = rsi;
            rsiHighL3.Indicator = rsi;

            rsiLowL1.Indicator = rsi;
            rsiLowL2.Indicator = rsi;
            rsiLowL3.Indicator = rsi;


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

        public ProfitSignal CreateProfitSignal(string name, string symbol, PLSignalConfig config)
        {
            var signal = new ProfitSignal(name, symbol, this, config);
            return signal;
        }

        public GradientSignal CreateRsiPositionSignal(string name, string symbol, GradientSignalConfig config, BuySell action)
        {
            config.Tolerance = RsiGradientTolerance;
            config.LearnRate = RsiGradientLearnRate;
            config.AnalyseAverage = Average.Ema;
            config.CollectAverage = Average.Ema;
            var signal = new GradientSignal(name, symbol, this, config, action);
            return signal;
        }

        public void SetAnalyserDefaults(params AnalyserConfig[] configs)
        {
            foreach (var item in configs)
            {
                if (item.InitialCollectSize == 0) item.InitialCollectSize = DataCollectSize;
                if (item.InitialAnalyseSize == 0) item.InitialAnalyseSize = DataAnalysisSize;
                item.CollectAverage = DataCollectAverage;
                item.AnalyseAverage = DataAnalysisAverage;
            }
        }

        public void CreateSignals()
        {
            DateTime? trackStart = new DateTime(2022, 03, 17, 9, 30, 0);
            DateTime? trackFinish = new DateTime(2022, 03, 17, 11, 0, 0);

            //DateTime? trackStart = null;
            //DateTime? trackFinish = null;


            SetAnalyserDefaults(this.GetType().GetProperties().Where(p => typeof(AnalyserConfig).IsAssignableFrom(p.PropertyType)).Select(p => p.GetValue(this)).Select(p=>(AnalyserConfig)p).ToArray());

            
            this.Signals.Add(this.powerSignal = new PowerSignal("power", Symbol, this, VolumePowerConfig));
            this.Signals.Add(this.rsiValue = new IndicatorAnalyser("rsi", Symbol, this, RsiValueConfig));            

            this.Signals.Add(this.rsiHighL1 = CreateRsiPositionSignal("rsi-high-l1", Symbol, RsiHighL1Config, BuySell.Sell));
            this.Signals.Add(this.rsiHighL2 = CreateRsiPositionSignal("rsi-high-l2", Symbol, RsiHighL2Config, BuySell.Sell));
            this.Signals.Add(this.rsiHighL3 = CreateRsiPositionSignal("rsi-high-l3", Symbol, RsiHighL3Config, BuySell.Sell));
            
            this.Signals.Add(this.rsiLowL1 = CreateRsiPositionSignal("rsi-low-l1", Symbol, RsiLowL1Config, BuySell.Buy));
            this.Signals.Add(this.rsiLowL2 = CreateRsiPositionSignal("rsi-low-l2", Symbol, RsiLowL2Config, BuySell.Buy));
            this.Signals.Add(this.rsiLowL3 = CreateRsiPositionSignal("rsi-low-l3", Symbol, RsiLowL3Config, BuySell.Buy));

            this.Signals.Add(this.maCross = new CrossSignal("cross-ma59", Symbol, this, MaCrossConfig));


            this.Signals.Add(this.profitSignal = CreateProfitSignal("profit", Symbol, ProfitConfig));
            this.Signals.Add(this.rsiProfitSignal = CreateProfitSignal("rsi-profit", Symbol, RsiProfitConfig));
            this.rsiProfitSignal.LimitingSignalTypes.Add(typeof(GradientSignal));
            this.Signals.Add(this.rsiLossSignal = new LossSignal("loss", Symbol, this, RsiLossConfig));
            this.rsiLossSignal.LimitingSignalTypes.Add(typeof(GradientSignal));

            if (rsiLowL1.Enabled) this.rsiLossSignal.CostSignals.Add(rsiLowL1);
            if (rsiHighL1.Enabled) this.rsiLossSignal.CostSignals.Add(rsiHighL1);
            if (rsiLowL2.Enabled) this.rsiLossSignal.CostSignals.Add(rsiLowL2);
            if (rsiHighL2.Enabled) this.rsiLossSignal.CostSignals.Add(rsiHighL2);

            this.Signals.Add(this.closePositionsSignal = new ClosePositionsSignal("daily-close", Symbol, this, DailyCloseConfig));

            Fibonacci fib = this.PriceLowLimit > 0 ? new Fibonacci(PriceLowLimit, PriceHighLimit) : null;

            Signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
                p.PerfMon = this.Watch;

                var analyser = p as AnalyserBase<AnalyserConfig>;
                var profit = p as PLSignal;

                if (analyser != null)
                {
                    analyser.CollectSize = DataCollectSize;
                    analyser.TrackStart = trackStart ?? analyser.TrackStart;
                    analyser.TrackEnd = trackFinish ?? analyser.TrackEnd;
                }

                if (profit != null)
                {
                    profit.FibonacciLevels = fib;
                }
            });

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

            if (maCross != null)
            {
                this.Watch.AddFilter($"{maCross.Name}/sensitivity", 10);
            }
            if (powerSignal != null)
            {
                this.Watch.AddFilter($"{powerSignal.Name}/volume", 10);
                this.Watch.AddFilter($"{powerSignal.Name}/VolumePerSecond", 10);
            }
            base.ConfigureMonitor();
        }

        public override void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            CreateSignals();
            base.Init();
        }


        private void HandleProfitLossSignal(PLSignal signal, ProfitLossResult result)
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
            quantity = Math.Min(portfolio.Quantity, remaining >= signal.Config.KeepQuantity ? quantity : portfolio.Quantity - keep);

            if (quantity > 0)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, OriginalPrice: {result.OriginalPrice} MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Info, result.SignalTime);
                sendOrder(Symbol, quantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? OrderIcon.TakeProfit : OrderIcon.StopLoss, result.SignalTime, result);
            }
        }





        public override void Decide(SignalBase signal, SignalEventArgs data)
        {
            if (data.Result.SignalTime.Second % 30 == 0 && !Simulation)
            {
                Log($"Process [{data.Result.Signal.Name}] using {data.Result} from ", LogLevel.Debug);
            }
            if (!WaitForOrder("Decide")) return;

            var result = data.Result;

            Log($"Starting to process signal as {data.Result} from {data.Result.Signal.Name}", LogLevel.Verbose);
            if (result.Signal is PLSignal)
            {
                HandleProfitLossSignal((PLSignal)result.Signal, (ProfitLossResult)result);
            }
            else if ((result.Signal.Name.StartsWith("rsi") && result.Signal is GradientSignal))
            {
                HandleRsiLimitSignal((GradientSignal)result.Signal, (GradientSignalResult)result);
            }
            else if (result.Signal is CrossSignal)
            {
                HandleCrossSignal((CrossSignal)result.Signal, (CrossSignalResult)result);
            }
            else if (result.Signal.Name == "daily-close")
            {
                HandleDailyCloseSignal((ClosePositionsSignal)result.Signal, (ClosePositionsSignalResult)result);
            }
        }

        private void HandleRsiLimitSignal(GradientSignal signal, GradientSignalResult signalResult)
        {
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);
            var lastOrder = portfolio.CompletedOrders.LastOrDefault();
            

            var rsiOrders = portfolio.GetLastPositionOrders(typeof(GradientSignal));
            var lastOrderIsLoss = portfolio.LastOrderIsLoss;
            if (lastOrderIsLoss && rsiOrders.Count > 0) return;
            var keepPosition = false;

            Log($"HandleRsiLimit: {signalResult.finalResult} {portfolio.IsLong} {portfolio.IsShort} {keepPosition} {signalResult.Gradient.UsedValue}", LogLevel.Verbose);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong && keepPosition) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort && keepPosition) return;

            RsiOrderConfig config = signal == rsiHighL1 || signal == rsiLowL1 ? RsiOrderL1: 
                (signal == rsiHighL2 || signal == rsiLowL2 ? RsiOrderL2 : RsiOrderL3);

            if (config.Action == RsiPositionAction.OpenIfEmpty && !portfolio.IsEmpty) return;

            if (config.Action == RsiPositionAction.BuyAdditional && !portfolio.IsEmpty && portfolio.Side != signalResult.finalResult) return;
            if (config.Action == RsiPositionAction.BuyAdditional && portfolio.Quantity >= config.Quantity) return;

            var orderQuantity = (config.Action == RsiPositionAction.BuyAdditional || config.Action == RsiPositionAction.OpenIfEmpty) ? config.Quantity: portfolio.Quantity + config.Quantity;

            if (!keepPosition && !portfolio.IsEmpty && portfolio.Side == signalResult.finalResult)
            {
                orderQuantity = config.Quantity - portfolio.Quantity;
            }

            if (orderQuantity > 0)
            {
                sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"{signal.Name}[{signalResult}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            }
        }

        public void MakePortfolio(string symbol, decimal quantity, BuySell side, string comment, SignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            var orderQuantity = quantity;            
            
            if (!portfolio.IsEmpty)
            {
                orderQuantity = portfolio.Side == side ? quantity - portfolio.Quantity: portfolio.Quantity + quantity;
                if (orderQuantity < 0) {
                    side = side == BuySell.Buy ? BuySell.Sell : BuySell.Buy;
                    orderQuantity = Math.Abs(orderQuantity);
                }
            }

            if (orderQuantity > 0) sendOrder(Symbol, orderQuantity, side, comment, 0, OrderIcon.PositionClose, Now, result, false);

        }

        public void HandleDailyCloseSignal(ClosePositionsSignal signal, ClosePositionsSignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);


            if (result.Quantity == 0 && !portfolio.IsEmpty) ClosePositions(Symbol, result);
            else
            {
                var macd = maCross.i1k.Results.Last().Value.Value;
                var expectedSide = macd > 0 ? BuySell.Buy : BuySell.Sell;
                MakePortfolio(Symbol, result.Quantity, expectedSide, "daily close", result);
            }                        
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

                sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}/{cross.AvgChange.ToCurrency()},{cross.AnalyseSize}, {(currentRsi.HasValue ? currentRsi.Value.ToCurrency() : 0)}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            }
        }

        public override void CompletedOrder(ExchangeOrder order)
        {

            var signal = order.SignalResult.Signal;

            var tp = signal as PLSignal;
            var cross = order.SignalResult as CrossSignalResult;
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);

            signal.AddOrder(1, order.Quantity);

            if (tp != null)
            {
                tp.IncrementParams();
            }
            else
            {
                Signals.Where(p => p is PLSignal).Select(p => (PLSignal)p).ToList().ForEach(p => p.ResetOrders());
            }
            if (portfolio.IsEmpty)
            {
                if (portfolio.LastOrderIsLoss && portfolio.IsLastPositionOrderInstanceOf(typeof(GradientSignal)))
                {
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