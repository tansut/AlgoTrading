// algo
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kalitte.Trading.Algos
{
    public interface IAlgoParameter
    {
        Bist30 Algo { get; set; }
    }

    public enum RsiPositionAction
    {
        None = 0,
        IfEmpty = 1,
        Additional = 2,
        Radical = 4
    }


    public enum ClosePositionSide
    {
        UseCross,
        KeepSide
    }

    public class DailyCloseConfig : ClosePositionsSignalConfig
    {

    }

    public class OrderConfig : ConfigParameters
    {
        [AlgoParam(10)]
        public decimal Total { get; set; }

        [AlgoParam(false)]
        public bool PLEnabled { get; set; }


        [AlgoParam(null)]
        public decimal[] PL { get; set; }

        [AlgoParam(null)]
        public decimal[] PLMultiplier { get; set; }


        [AlgoParam(0.5)]
        public decimal NightRatio { get; set; }

        [AlgoParam(0.5)]
        public decimal KeepRatio { get; set; }

        [AlgoParam(ClosePositionSide.KeepSide)]
        public ClosePositionSide KeepSide { get; set; }
    }

    public class RsiOrderConfig : GradientSignalConfig, IAlgoParameter
    {
        public Bist30 Algo { get; set; }

        [AlgoParam(0)]
        public decimal KeepRatio { get; set; }

        [AlgoParam(0)]
        public decimal MakeRatio { get; set; }

        public decimal Keep
        {
            get
            {
                return Algo.RoundQuantity(Algo.OrderConfig.Total * KeepRatio);
            }
        }

        public decimal Make
        {
            get
            {
                return Algo.RoundQuantity(Algo.OrderConfig.Total * MakeRatio);
            }
        }

        [AlgoParam(RsiPositionAction.IfEmpty)]
        public RsiPositionAction Action { get; set; }
    }

    public class CrossOrderConfig : CrossSignalConfig, IAlgoParameter
    {
        public Bist30 Algo { get; set; }

        [AlgoParam(false)]
        public bool RsiLongEnabled { get; set; }

        [AlgoParam(false)]
        public bool RsiShortEnabled { get; set; }

        [AlgoParam(0)]
        public decimal PreOrder { get; set; }

        [AlgoParam(null)]
        public decimal[] RsiLong { get; set; }

        [AlgoParam(null)]
        public decimal[] RsiShort { get; set; }

        [AlgoParam(null)]
        public decimal[] RsiLongMultiplier { get; set; }

        [AlgoParam(null)]
        public decimal[] RsiShortMultiplier { get; set; }



        [AlgoParam(0)]
        public decimal QuantityRatio { get; set; }

        public decimal Quantity
        {
            get
            {
                return Algo.RoundQuantity(Algo.OrderConfig.Total * QuantityRatio);
            }
        }
    }


    public class Bist30 : AlgoBase
    {
        public decimal InitialQuantity { get; set; }

        [AlgoParam(null, "Orders")]
        public OrderConfig OrderConfig { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderHighL1 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderHighL2 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderHighL3 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderLowL1 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderLowL2 { get; set; }

        [AlgoParam(null)]
        public RsiOrderConfig RsiOrderLowL3 { get; set; }


        // ma cross
        [AlgoParam(5)]
        public int MovPeriod { get; set; }
        [AlgoParam(9)]
        public int MovPeriod2 { get; set; }


        // fibonachi
        [AlgoParam(0)]
        public decimal PriceLowLimit { get; set; }
        [AlgoParam(0)]
        public decimal PriceHighLimit { get; set; }


        [AlgoParam(null, "RsiLoss")]
        public PLSignalConfig RsiLossConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "Profit")]
        public PLSignalConfig ProfitConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "CrossLoss")]
        public PLSignalConfig CrossLossConfig { get; set; } = new PLSignalConfig();

        [AlgoParam(null, "GlobalLoss")]
        public PLSignalConfig GlobalLossConfig { get; set; } = new PLSignalConfig();


        [AlgoParam(null, "CrossL1")]
        public CrossOrderConfig CrossL1Config { get; set; } = new CrossOrderConfig() { };


        [AlgoParam(null, "VolumePower")]
        public PowerSignalConfig VolumePowerConfig { get; set; } = new PowerSignalConfig();

        [AlgoParam(null, "RsiValue")]
        public AnalyserConfig RsiValueConfig { get; set; } = new AnalyserConfig();

        [AlgoParam(null, "DailyClose")]
        public DailyCloseConfig DailyCloseConfig { get; set; }

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
        [AlgoParam(120)]
        public int DataAnalysisLookback { get; set; }
        [AlgoParam(BarPeriod.Sec5)]
        public BarPeriod DataAnalysisPeriods { get; set; }



        [AlgoParam(0.015)]
        public decimal RsiGradientTolerance { get; set; }
        [AlgoParam(0.005)]
        public decimal RsiGradientLearnRate { get; set; }




        CrossSignal maCrossL1 = null;

        ProfitSignal profitSignal = null;
        LossSignal rsiLossSignal = null;
        LossSignal crossLossSignal = null;
        LossSignal globalLossSignal = null;

        IndicatorAnalyser rsiValue = null;
        PowerSignal powerSignal = null;
        ClosePositionsSignal closePositionsSignal = null;

        GradientSignal rsiHighL1;
        GradientSignal rsiHighL2;
        GradientSignal rsiHighL3;

        GradientSignal rsiLowL1;
        GradientSignal rsiLowL2;
        GradientSignal rsiLowL3;

        CrossSignalResult usedCross4PreOrder = null;


        public void InitSignals()
        {
            var periodData = GetSymbolData(this.Symbol, this.SymbolPeriod);

            powerSignal.Indicator = new Rsi(periodData.Periods, PowerLookback, CandlePart.Volume);
            maCrossL1.i1k = new Macd(periodData.Periods, MovPeriod, MovPeriod2, 9);
            maCrossL1.i2k = new Custom((q) => 0, periodData.Periods);
            maCrossL1.PowerSignal = powerSignal;

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
            var time = t ?? Now;
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
                if (item.CollectSize == 0) item.CollectSize = DataCollectSize;
                if (item.AnalyseSize == 0) item.AnalyseSize = DataAnalysisSize;
                item.CollectAverage = DataCollectAverage;
                item.AnalyseAverage = DataAnalysisAverage;
                if (item.Lookback == 0) item.Lookback = DataAnalysisLookback;
                if (item.AnalysePeriod == BarPeriod.NotSet) item.AnalysePeriod = DataAnalysisPeriods;
            }
        }

        public void CreateSignals()
        {
            DateTime? trackStart = null;
            DateTime? trackFinish = null;

            //trackStart = new DateTime(2022, 03, 16, 9, 30, 0);
            //trackFinish = new DateTime(2022, 03, 16, 17, 0, 0);



            SetAnalyserDefaults(this.GetType().GetProperties().Where(p => typeof(AnalyserConfig).IsAssignableFrom(p.PropertyType)).Select(p => p.GetValue(this)).Select(p => (AnalyserConfig)p).ToArray());


            this.Signals.Add(this.powerSignal = new PowerSignal("power", Symbol, this, VolumePowerConfig));
            this.Signals.Add(this.rsiValue = new IndicatorAnalyser("rsi", Symbol, this, RsiValueConfig));

            this.Signals.Add(this.rsiHighL1 = CreateRsiPositionSignal("rsi-high-l1", Symbol, RsiOrderHighL1, BuySell.Sell));
            this.Signals.Add(this.rsiHighL2 = CreateRsiPositionSignal("rsi-high-l2", Symbol, RsiOrderHighL2, BuySell.Sell));
            this.Signals.Add(this.rsiHighL3 = CreateRsiPositionSignal("rsi-high-l3", Symbol, RsiOrderHighL3, BuySell.Sell));

            this.Signals.Add(this.rsiLowL1 = CreateRsiPositionSignal("rsi-low-l1", Symbol, RsiOrderLowL1, BuySell.Buy));
            this.Signals.Add(this.rsiLowL2 = CreateRsiPositionSignal("rsi-low-l2", Symbol, RsiOrderLowL2, BuySell.Buy));
            this.Signals.Add(this.rsiLowL3 = CreateRsiPositionSignal("rsi-low-l3", Symbol, RsiOrderLowL3, BuySell.Buy));

            this.Signals.Add(this.maCrossL1 = new CrossSignal("ema59-L1", Symbol, this, CrossL1Config));


            this.Signals.Add(this.profitSignal = CreateProfitSignal("profit", Symbol, ProfitConfig));
            this.Signals.Add(this.rsiLossSignal = new LossSignal("rsi-loss", Symbol, this, RsiLossConfig));
            this.rsiLossSignal.LimitingSignalTypes.Add(typeof(GradientSignal));

            this.Signals.Add(this.crossLossSignal = new LossSignal("cross-loss", Symbol, this, CrossLossConfig));
            this.crossLossSignal.LimitingSignalTypes.Add(typeof(CrossSignal));
            this.crossLossSignal.CostSignals.Add(maCrossL1);

            this.Signals.Add(this.globalLossSignal = new LossSignal("global-loss", Symbol, this, GlobalLossConfig));

            if (rsiHighL1.Enabled) this.rsiLossSignal.CostSignals.Add(rsiHighL1);
            if (rsiHighL2.Enabled) this.rsiLossSignal.CostSignals.Add(rsiHighL2);
            if (rsiHighL3.Enabled) this.rsiLossSignal.CostSignals.Add(rsiHighL3);
            if (rsiLowL1.Enabled) this.rsiLossSignal.CostSignals.Add(rsiLowL1);
            if (rsiLowL2.Enabled) this.rsiLossSignal.CostSignals.Add(rsiLowL2);
            if (rsiLowL3.Enabled) this.rsiLossSignal.CostSignals.Add(rsiLowL3);

            this.Signals.Add(this.closePositionsSignal = new ClosePositionsSignal("daily-close", Symbol, this, DailyCloseConfig));

            Fibonacci fib = this.PriceLowLimit > 0 ? new Fibonacci(PriceLowLimit, PriceHighLimit) : null;

            Signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.OnSignal += SignalReceieved;
                p.PerfMon = this.Watch;

                var analyser = p as AnalyserBase<AnalyserConfig>;
                var profit = p as PLSignal;

                if (analyser != null)
                {
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
                    sendOrder(symbol, item.Value.Quantity, item.Value.Side == BuySell.Buy ? BuySell.Sell : BuySell.Buy, "close position", signalResult, OrderUsage.ClosePosition, true);
                }
            }
        }



        public override void ConfigureMonitor()
        {

            if (maCrossL1 != null)
            {
                this.Watch.AddFilter($"{maCrossL1.Name}/sensitivity", 10);
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
            this.GetType().GetProperties().Where(p => typeof(ConfigParameters).IsAssignableFrom(p.PropertyType)).Select(p => p.GetValue(this)).Where(c => c is IAlgoParameter).Select(p => (IAlgoParameter)p).ToList().ForEach(c => c.Algo = this);
            this.InitialQuantity = OrderConfig.Total;
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            CreateSignals();
            base.Init();
        }


        public void CheckNightSettings()
        {
            if (Now.Hour >= 19 && OrderConfig.NightRatio != 0)
            {
                var newTarget = RoundQuantity(InitialQuantity * OrderConfig.NightRatio);
                if (newTarget < OrderConfig.Total)
                {
                    OrderConfig.Total = newTarget;
                    Log($"Set order count to {OrderConfig.Total}, night", LogLevel.Debug);
                }
            }
        }

        private void HandleProfitLossSignal(PLSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (portfolio.IsEmpty) return;
            if (result.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (result.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var lastSignalTime = portfolio.LastPositionOrder == null ? DateTime.MinValue : portfolio.LastPositionOrder.SignalResult.SignalTime;
            if (signal.Usage == OrderUsage.StopLoss && (result.SignalTime - lastSignalTime).TotalSeconds < 60)
            {
                Log($"{signal.Name} {result} received but there is no time dif between {lastSignalTime} and {result.SignalTime}", LogLevel.Warning);
            }


            decimal keep = result.KeepQuantity;
            decimal quantity = result.Quantity;
            decimal remaining = portfolio.Quantity - quantity;
            quantity = Math.Min(portfolio.Quantity, remaining >= keep ? quantity : portfolio.Quantity - keep);

            if (quantity > 0)
            {
                Log($"[{result.Signal.Name} received]: PL: {result.PL}, OriginalPrice: {result.OriginalPrice} MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Info, result.SignalTime);
                sendOrder(Symbol, quantity, result.finalResult.Value, $"[{result.Signal.Name}], Change:{result.UsedPriceChange}% PL: {Math.Abs(result.PL)}", result);
            }
        }

        public bool CheckBeforeDecide(bool ignoreOrderLimits)
        {
            CheckNightSettings();
            if (OrderConfig.Total == 0 && !ignoreOrderLimits)
            {
                if (!UserPortfolioList.GetPortfolio(Symbol).IsEmpty) ClosePositions(Symbol, null);
                return false;
            }
            if (WaitingOrderExpired()) CancelCurrentOrder("Cannot get a result from broker");
            if (!WaitForOrder("Decide")) return false;

            return true;
        }



        public override void Decide(SignalBase signal, SignalEventArgs data)
        {
            if (data.Result.SignalTime.Second % 30 == 0 && !Simulation)
            {
                Log($"Process [{data.Result.Signal.Name}] using {data.Result} from ", LogLevel.Debug);
            }

            if (!CheckBeforeDecide(signal is ClosePositionsSignal)) return;

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

            var rsiOrders = portfolio.GetLastPositionOrders(new[] { typeof(GradientSignal) });
            var lastOrderIsLoss = portfolio.LastOrderIsLoss;
            if (lastOrderIsLoss && rsiOrders.Count > 0) return;
            var keepPosition = false;

            Log($"HandleRsiLimit: {signalResult.finalResult} {portfolio.IsLong} {portfolio.IsShort} {keepPosition} {signalResult.Gradient.UsedValue}", LogLevel.Verbose);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong && keepPosition) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort && keepPosition) return;

            RsiOrderConfig config = (RsiOrderConfig)signal.Config;
            var usage = config.Usage;

            var portfolioSide = portfolio.IsEmpty ? signalResult.finalResult.Value : portfolio.Side;
            if (config.Action == RsiPositionAction.None) return;
            if (config.Action == RsiPositionAction.IfEmpty && !portfolio.IsEmpty) return;

            decimal finalQuantity = config.Make;
            BuySell finalPosition = portfolioSide;

            if (config.Action == RsiPositionAction.Additional && portfolioSide != signalResult.finalResult.Value && config.Keep >= 0)
            {
                usage = OrderUsage.TakeProfit;
                finalQuantity = config.Keep;
            }
            else if (config.Action == RsiPositionAction.Additional && portfolioSide == signalResult.finalResult.Value)
                finalQuantity = Math.Max(portfolio.Quantity, config.Make);
            else if (config.Action == RsiPositionAction.Radical)
                finalPosition = signalResult.finalResult.Value;

            MakePortfolio(Symbol, finalQuantity, finalPosition, $"{signal.Name}[{signalResult}]", signalResult, usage);
        }

        public void MakePortfolio(string symbol, decimal quantity, BuySell side, string comment, SignalResult result, OrderUsage usage = OrderUsage.Unknown)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            var orderQuantity = quantity;

            if (!portfolio.IsEmpty)
            {
                orderQuantity = portfolio.Side == side ? quantity - portfolio.Quantity : portfolio.Quantity + quantity;
                if (orderQuantity < 0)
                {
                    side = side == BuySell.Buy ? BuySell.Sell : BuySell.Buy;
                    orderQuantity = Math.Abs(orderQuantity);
                }
            }

            if (orderQuantity > 0)
            {
                sendOrder(Symbol, orderQuantity, side, comment, result, usage);
            }
        }

        public void HandleDailyCloseSignal(ClosePositionsSignal signal, ClosePositionsSignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            var expectedSide = portfolio.Side;
            if (OrderConfig.KeepSide == ClosePositionSide.UseCross)
            {
                var macd = maCrossL1.i1k.Results.Last().Value.Value;
                expectedSide = macd > 0 ? BuySell.Buy : BuySell.Sell;
            }
            var usage = expectedSide == portfolio.Side ? OrderUsage.CreatePosition : OrderUsage.CreatePosition;
            MakePortfolio(Symbol, RoundQuantity(this.InitialQuantity * OrderConfig.KeepRatio), expectedSide, $"daily close", result, usage);
        }

        public void HandleCrossSignal(CrossSignal signal, CrossSignalResult signalResult)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);
            var lastOrder = portfolio.CompletedOrders.LastOrDefault();
            var lastPositionOrder = portfolio.LastPositionOrder;
            var config = (CrossOrderConfig)signal.Config;

            var currentRsi = 0M;

            var rsiOrderMultiplier = 1M;

            var rsi = rsiValue.LastSignalResult as IndicatorAnalyserResult;
            if (rsi != null && rsi.Value.HasValue)
            {
                currentRsi = rsi.Value.Value;

                if (!signalResult.MorningSignal && config.RsiLongEnabled && signalResult.finalResult == BuySell.Buy)
                {
                    rsiOrderMultiplier = Helper.GetMultiplier(currentRsi, config.RsiLong, config.RsiLongMultiplier);
                    if (rsiOrderMultiplier < 0 && signalResult.finalResult.HasValue)
                    {
                        crossLossSignal.Enabled = true;
                        return;
                    }
                }
                else if (!signalResult.MorningSignal && config.RsiShortEnabled && signalResult.finalResult == BuySell.Sell)
                {
                    rsiOrderMultiplier = Helper.GetMultiplier(currentRsi, config.RsiShort, config.RsiShortMultiplier);
                    if (rsiOrderMultiplier < 0 && signalResult.finalResult.HasValue)
                    {
                        crossLossSignal.Enabled= true;
                        return;
                    }
                }
            }


            if (usedCross4PreOrder == null && config.PreOrder < 1 && lastOrder != null && lastOrder.SignalResult.Signal == signal && signalResult.preResult.HasValue && !portfolio.IsEmpty && portfolio.Side != signalResult.preResult)
            {
                usedCross4PreOrder = signalResult;
                MakePortfolio(Symbol, RoundQuantity(portfolio.Quantity * config.PreOrder), portfolio.Side, $"[{signalResult.Signal.Name}*/{signal.AvgChange.ToCurrency()},{signal.Lookback}, rsi:{currentRsi.ToCurrency()}]", signalResult);
                return;
            }

            if (!signalResult.finalResult.HasValue)
                return;

            var keepPosition = lastPositionOrder != null && lastPositionOrder.SignalResult.Signal == signal;

            Log($"HandleCross: {signalResult.Rsi} {signalResult.finalResult} {portfolio.IsLong} {portfolio.IsShort} {keepPosition}", LogLevel.Verbose);
            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong && keepPosition) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort && keepPosition) return;




            var quantity = RoundQuantity(rsiOrderMultiplier * config.Quantity);


            MakePortfolio(Symbol, quantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}{(signal.Config != config ? "*" : "")}/{signal.AvgChange.ToCurrency()},{signal.Lookback}, rsi:{currentRsi.ToCurrency()}]", signalResult);

        }


        public void AdjustDailyProfitLoss()
        {
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);
            var stats = portfolio.GetDailyStats(Now.Date);
            decimal target = this.OrderConfig.Total;

            if (OrderConfig.PLEnabled)
            {
                var ratio = Helper.GetMultiplier(stats.NetPl, OrderConfig.PL, OrderConfig.PLMultiplier);
                var newtarget = RoundQuantity(InitialQuantity * ratio);
                if (newtarget < target) target = newtarget;
            }

            if (OrderConfig.Total != target)
            {
                Log($"Daily order limit adjusted to {target} for {Now.Date}, Todays orders: {stats.Total}, NetPL: {stats.NetPl}", LogLevel.Test);
                this.OrderConfig.Total = target;
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
                tp.IncrementParams(order);
            }
            else
            {
                Signals.Where(p => p is PLSignal).Select(p => (PLSignal)p).ToList().ForEach(p => p.ResetOrders());
            }

            if (cross != null && cross.finalResult.HasValue)
            {
                usedCross4PreOrder = null;
            } else if (cross != null)
            {
                crossLossSignal.Enabled = false;
            }
            //if (portfolio.IsEmpty)
            //{
            //    if (portfolio.LastOrderIsLoss && portfolio.IsLastPositionOrderInstanceOf(typeof(GradientSignal)))
            //    {
            //        Log($"Skipping cross reset since last order is position close by loss/rsi", LogLevel.Debug);
            //    }
            //    else Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.ResetCross());
            //}

            AdjustDailyProfitLoss();

            base.CompletedOrder(order);
        }



        public override void DayStart()
        {
            this.OrderConfig.Total = InitialQuantity;
            Signals.ForEach(p => p.Reset());
            var portfolio = UserPortfolioList.GetPortfolio(Symbol);
            portfolio.CompletedOrders.Clear();
            InitializePositions(new List<PortfolioItem> { portfolio }, true);

        }

        public override void InitializePositions(List<PortfolioItem> portfolioItems, bool keepPortfolio = false)
        {
            base.InitializePositions(portfolioItems, keepPortfolio);
            //var portfolio = UserPortfolioList.GetPortfolio(Symbol);
            //var lastOrder = portfolio.CompletedOrders.LastOrDefault();
            //if (lastOrder != null && lastOrder.SignalResult.Signal is CrossSignal)
            //{
            //    ((CrossSignal)lastOrder.SignalResult.Signal).FirstCrossRequired = false;
            //    Log($"First cross disabled since last order was cross", LogLevel.Debug);
            //}
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
