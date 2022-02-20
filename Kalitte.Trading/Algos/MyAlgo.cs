// algo
using Kalitte.Trading.Indicators;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Kalitte.Trading.Algos
{
    public class MyAlgo : AlgoBase
    {




        [AlgoParam(2)]
        public decimal OrderQuantity { get; set; } = 2M;


        [AlgoParam(2)]
        public int CrossPriceCollectionPeriod { get; set; } = 2;




        [AlgoParam(true)]
        public bool UseSmaForCross { get; set; } = true;

        [AlgoParam(5)]
        public int MovPeriod { get; set; } = 5;

        [AlgoParam(9)]
        public int MovPeriod2 { get; set; } = 9;

        [AlgoParam(true)]
        public bool DynamicCross { get; set; } = true;


        [AlgoParam(0.15)]
        public decimal MaAvgChange { get; set; } = 0.15M;

        [AlgoParam(15)]
        public int MaPeriods { get; set; } = 15;


        [AlgoParam(0)]
        public decimal ExpectedNetPl { get; set; } = 0;



        [AlgoParam(false)]
        public bool SimulateOrderSignal { get; set; } = false;

        [AlgoParam(1)]
        public decimal ProfitQuantity { get; set; } = 1;

        [AlgoParam(0)]
        public decimal LossQuantity { get; set; } = 0;

        [AlgoParam(16)]
        public decimal ProfitPuan { get; set; } = 16;

        [AlgoParam(4)]
        public decimal LossPuan { get; set; } = 4;

        [AlgoParam(4)]
        public int RsiPriceCollectionPeriod { get; set; } = 4;

        [AlgoParam(60)]
        public decimal RsiHighLimit { get; set; } = 60M;

        [AlgoParam(40)]
        public decimal RsiLowLimit { get; set; } = 40M;

        [AlgoParam(2)]
        public decimal MinRsiChange { get; set; } = 2M;


        [AlgoParam(1)]
        public decimal RsiProfitQuantity { get; set; } = 1M;

        [AlgoParam(2)]
        public decimal RsiProfitPuan { get; set; } = 2M;



        [AlgoParam(false)]
        public bool AlwaysGetRsiProfit { get; set; } = false;


        [AlgoParam(14)]
        public int Rsi { get; set; } = 14;

        [AlgoParam(60)]
        public int RsiAnalysisPeriod { get; set; } = 60;

        [AlgoParam(0)]
        public int MACDShortPeriod { get; set; } = 0;

        [AlgoParam(9)]
        public int MACDLongPeriod { get; set; } = 9;

        [AlgoParam(0.05)]
        public decimal MacdAvgChange { get; set; } = 0.05M;

        [AlgoParam(15)]
        public int MacdPeriods { get; set; } = 15;

        [AlgoParam(9)]
        public int MACDTrigger { get; set; } = 9;

        [AlgoParam(false)]
        public bool AlwaysGetProfit { get; set; } = false;

        [AlgoParam(false)]
        public bool AlwaysStopLoss { get; set; } = false;



        public FinanceBars MinBars = null;

        CrossSignal maSignal = null;
        TakeProfitOrLossSignal takeProfitSignal = null;
        TrendSignal rsiTrendSignal = null;
        TrendSignal priceTrend = null;
        TrendSignal atrTrend = null;

        public override void InitMySignals(DateTime t)
        {

            var price = new Price(PeriodBars, 9);
            priceTrend.i1k = price;

            var atr = new Atr(PeriodBars, 2);
            atrTrend.i1k = atr;

            if (maSignal != null)
            {
                var movema5 = new Ema(PeriodBars, MovPeriod);
                var mov2ema9 = new Ema(PeriodBars, MovPeriod2);
                maSignal.i1k = movema5;
                maSignal.i2k = mov2ema9;
            }


            var macds = Signals.Where(p => p.Name == "cross:macd593").FirstOrDefault() as CrossSignal;

            if (macds != null)
            {

                var macdi = new Macd(PeriodBars, MACDShortPeriod, MACDLongPeriod, MACDTrigger);

                macds.i1k = macdi;
                macds.i2k = macdi.Trigger;
            }

            if (rsiTrendSignal != null)
            {
                var rsi = new Rsi(PeriodBars, Rsi);
                rsiTrendSignal.i1k = rsi;
            }



            Signals.ForEach(p =>
            {
                p.Init();
            });

        }


        public override void InitializeBars(string symbol, BarPeriod period, DateTime t)
        {
            //this.MinBars = GetPeriodBars(symbol, period, t);
            base.InitializeBars(symbol, period, t);
        }

        public void InitSignals()
        {
            this.priceTrend = new TrendSignal("price-trend", Symbol, this);
            priceTrend.Periods = 2;
            priceTrend.PriceCollectionPeriod = 1;
            priceTrend.ReferenceType = TrendReference.LastCheck;

            this.atrTrend = new TrendSignal("atr-trend", Symbol, this);
            atrTrend.Periods = 45;
            atrTrend.PriceCollectionPeriod = 2;
            atrTrend.HowToReset = ResetList.Always;


            this.Signals.Add(this.priceTrend);
            this.Signals.Add(this.atrTrend);


            if (MovPeriod > 0 && !SimulateOrderSignal)
            {
                this.maSignal = new CrossSignal("cross:ma59", Symbol, this) { UseSma = UseSmaForCross, PriceCollectionPeriod = CrossPriceCollectionPeriod, AvgChange = MaAvgChange, Periods = MaPeriods };
                this.Signals.Add(maSignal);
            }

            if (MACDShortPeriod > 0 && !SimulateOrderSignal)
            {
                var macds = new CrossSignal("cross:macd593", Symbol, this) { UseSma = UseSmaForCross, PriceCollectionPeriod = CrossPriceCollectionPeriod, AvgChange = MacdAvgChange, Periods = MacdPeriods };
                this.Signals.Add(macds);
            }



            if (SimulateOrderSignal) this.Signals.Add(new FlipFlopSignal("flipflop", Symbol, this, BuySell.Buy));
            if (!SimulateOrderSignal && (this.ProfitQuantity > 0 || this.LossQuantity > 0))
            {
                this.takeProfitSignal = new TakeProfitOrLossSignal("profitOrLoss", Symbol, this, this.ProfitPuan, this.ProfitQuantity, this.LossPuan, this.LossQuantity);
                this.Signals.Add(takeProfitSignal);
            }
            if (!SimulateOrderSignal && (RsiHighLimit > 0 || RsiLowLimit > 0))
            {
                rsiTrendSignal = new TrendSignal("rsi-trend", Symbol, this, RsiLowLimit == 0 ? new decimal?() : RsiLowLimit, RsiHighLimit == 0 ? new decimal() : RsiHighLimit) { UseSma = this.UseSmaForCross, Periods = RsiAnalysisPeriod, PriceCollectionPeriod = this.RsiPriceCollectionPeriod };
                this.Signals.Add(rsiTrendSignal);
            }

            Signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
            });
        }

        public override void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            InitSignals();
            base.Init();
        }

        public override void Stop()
        {
            base.Stop();
            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;

            if (Simulation && ExpectedNetPl > 0 && netPL < ExpectedNetPl) File.Delete(LogFile);
            else if (Simulation) Process.Start(LogFile);
        }



        private void HandleProfitLossSignal(TakeProfitOrLossSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (result.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (result.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var pq = portfolio.Quantity;

            bool doAction = pq == this.OrderQuantity;

            if (result.Direction == ProfitOrLoss.Profit && AlwaysGetProfit && pq > signal.ProfitQuantity) doAction = true;
            if (result.Direction == ProfitOrLoss.Loss && AlwaysStopLoss && pq > signal.LossQuantity) doAction = true;

            if (doAction)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Debug, result.SignalTime);
                sendOrder(Symbol, result.Direction == ProfitOrLoss.Profit ? signal.ProfitQuantity : signal.LossQuantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? OrderIcon.TakeProfit : OrderIcon.StopLoss, result.SignalTime, result);
            }
            //else Log($"[{result.Signal.Name}:{result.Direction}] received but quantity doesnot match. Portfolio: {pq} oq: {this.OrderQuantity}", LogLevel.Verbose, result.SignalTime);
        }



        private void HandlePriceTrendSignal(TrendSignal signal, TrendSignalResult result)
        {

        }


        public VolatileRatio EstimateVolatility(decimal val)
        {
            if (val < 0.125M) return VolatileRatio.Low;
            else if (val < 0.25M) return VolatileRatio.BelowAverage;
            else if (val < 0.50M) return VolatileRatio.Average;
            else if (val < 0.75M) return VolatileRatio.High;
            else return VolatileRatio.Critical;
        }

        private void HandleAtrTrendSignal(TrendSignal signal, TrendSignalResult result)
        {
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
                    maSignal.InOperationLock.WaitOne();
                    maSignal.InOperationLock.Reset();
                    try
                    {
                        maSignal.AdjustSensitivity(ratio, $"{VolatileRatio}({result.Trend.NewValue.ToCurrency()})");
                    }
                    finally
                    {
                        maSignal.InOperationLock.Set();
                    }

                    //Log($"Current volatility level: {VolatileRatio}. Adjusted cross to {maSignal.AvgChange}, {maSignal.Periods}. Signal: {result}", LogLevel.Critical, result.SignalTime);
                }
            }
        }

        private void HandleRsiTrendSignal(TrendSignal signal, TrendSignalResult result)
        {

            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            var marketPrice = GetMarketPrice(Symbol, result.SignalTime);

            if (marketPrice == 0)
            {
                Log($"{signal.Name} couldnot be executed since market price is zero", LogLevel.Warning, result.SignalTime);
                return;
            }

            //Log($"[rsi-trend]: {result}", LogLevel.Critical, result.SignalTime);

            if (!portfolio.IsEmpty)
            {
                var quantity = portfolio.Quantity <= OrderQuantity ? Math.Min(RsiProfitQuantity, portfolio.Quantity) : 0; // : RsiProfitQuantity;
                if (quantity > 0)
                {
                    var trend = result.Trend;
                    if (Math.Abs(trend.Change) < MinRsiChange) return;
                    if ((trend.Direction == TrendDirection.ReturnDown || trend.Direction == TrendDirection.MoreUp) && portfolio.IsLong && (portfolio.AvgCost + RsiProfitPuan) <= marketPrice)
                    {
                        if (maSignal != null) maSignal.Reset();
                        sendOrder(Symbol, quantity, BuySell.Sell, $"[{result.Signal.Name}:{trend.Direction}]", 0, OrderIcon.PositionClose, result.SignalTime, result);

                    }
                    else if ((trend.Direction == TrendDirection.ReturnUp || trend.Direction == TrendDirection.LessDown) && portfolio.IsShort && portfolio.AvgCost >= (marketPrice + RsiProfitPuan))
                    {
                        if (maSignal != null) maSignal.Reset();
                        sendOrder(Symbol, quantity, BuySell.Buy, $"[{result.Signal.Name}:{trend.Direction}]", 0, OrderIcon.PositionClose, result.SignalTime, result);
                    }
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
                if (result.Signal is TakeProfitOrLossSignal)
                {
                    var tpSignal = (TakeProfitOrLossSignal)(result.Signal);
                    var signalResult = (ProfitLossResult)result;
                    HandleProfitLossSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "rsi-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandleRsiTrendSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "price-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandlePriceTrendSignal(tpSignal, signalResult);
                }
                else if (result.Signal.Name == "atr-trend")
                {
                    var tpSignal = (TrendSignal)(result.Signal);
                    var signalResult = (TrendSignalResult)result;
                    HandleAtrTrendSignal(tpSignal, signalResult);
                }
                else HandleCrossSignal(signal, result);

            }
            finally
            {
                operationWait.Set();
            }
        }




        public void HandleCrossSignal(Signal signal, SignalResultX signalResult)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var orderQuantity = portfolio.Quantity + OrderQuantity;

            var cross = (CrossSignal)signalResult.Signal;
            sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}/{cross.AvgChange},{cross.Periods}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);

        }



        public override string ToString()
        {
            var assembly = typeof(MyAlgo).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }



    }
}
