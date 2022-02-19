// algo
using Kalitte.Trading.Indicators;
using Matriks.Lean.Algotrader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Algos
{
    public class MyAlgo: AlgoBase
    {




        [AlgoParam(2)]
        public decimal OrderQuantity { get; set; } = 2M;


        [AlgoParam(2)]
        public int CrossPriceCollectionPeriod { get; set; } = 2;


        [AlgoParam(4)]
        public int RsiPriceCollectionPeriod { get; set; } = 4;

        [AlgoParam(true)]
        public bool UseSmaForCross { get; set; } = true;

        [AlgoParam(5)]
        public int MovPeriod { get; set; } = 5;

        [AlgoParam(9)]
        public int MovPeriod2 { get; set; } = 9;


        [AlgoParam(0.15)]
        public decimal MaAvgChange { get; set; } = 0.15M;

        [AlgoParam(15)]
        public int MaPeriods { get; set; } = 15;


        [AlgoParam(0)]
        public decimal ExpectedNetPl { get; set; } = 0;


        [AlgoParam(true)]
        public bool DoublePositions { get; set; } = true;

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

        [AlgoParam(65)]
        public decimal RsiHighLimit { get; set; } = 72M;

        [AlgoParam(35)]
        public decimal RsiLowLimit { get; set; } = 28M;
        
        [AlgoParam(3)]
        public decimal MinRsiChange { get; set; } = 3M;


        [AlgoParam(9)]
        public int Rsi { get; set; } = 9;

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

        CrossSignal maSignal = null;
        TakeProfitOrLossSignal takeProfitSignal = null;
        RangeSignal rsiRangeSignal = null;
        TrendSignal rsiTrendSignal = null;
        TrendSignal priceTrend = null;

        public override void InitMySignals(DateTime t)
        {

            var price = new Price(PeriodBars, 9);
            priceTrend.i1k = price;

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




            if (rsiRangeSignal != null)
            {
                var rsi = new Rsi(PeriodBars, Rsi);
                rsiRangeSignal.i1k = rsi;
                rsiTrendSignal.i1k = rsi;
            }


            Signals.ForEach(p =>
            {
                p.Init();
            });

        }

        public void InitSignals()
        {
            this.priceTrend = new TrendSignal("price-trend", Symbol, this);
            priceTrend.Periods = 2;
            priceTrend.PriceCollectionPeriod = 1;
            priceTrend.ReferenceType = TrendReference.LastCheck;

            this.Signals.Add(this.priceTrend);

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
                rsiRangeSignal = new RangeSignal("rsi", Symbol, this, RsiLowLimit == 0 ? new decimal?() : RsiLowLimit, RsiHighLimit == 0 ? new decimal() : RsiHighLimit) { AnalysisPeriod = RsiAnalysisPeriod };
                rsiTrendSignal = new TrendSignal("rsi-trend", Symbol, this, RsiLowLimit == 0 ? new decimal?() : RsiLowLimit, RsiHighLimit == 0 ? new decimal() : RsiHighLimit) { UseSma = this.UseSmaForCross, Periods = RsiAnalysisPeriod, PriceCollectionPeriod = this.RsiPriceCollectionPeriod };
                this.Signals.Add(rsiRangeSignal);
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


        private void HandleRsiSignal(RangeSignal signal, RangeSignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (result.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (result.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var pq = portfolio.Quantity;
            var marketPrice = GetMarketPrice(Symbol, result.SignalTime);

            if (marketPrice == 0)
            {
                Log($"{signal.Name} couldnot be executed since market price is zero", LogLevel.Warning, result.SignalTime);
                return;
            }

            if (!portfolio.IsEmpty && portfolio.Quantity < OrderQuantity)
            {
                Log($"[{result.Signal.Name}:{result.Status} {result.Value}] received.", LogLevel.Debug, result.SignalTime);
                if (portfolio.Side == BuySell.Sell && result.Status == RangeStatus.BelowMin && portfolio.AvgCost > marketPrice)
                {
                    //Log($"{signal.Name} simulate position close,  buy.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    //sendOrder(Symbol, portfolio.Quantity, OrderSide.Buy, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime, result);
                }
                else if (portfolio.Side == BuySell.Buy && result.Status == RangeStatus.AboveHigh && portfolio.AvgCost < marketPrice)
                {
                    //Log($"{signal.Name} simulate position close,  sell.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    //sendOrder(Symbol, portfolio.Quantity, OrderSide.Sell, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime, result);
                }
                //else Log($"{signal.Name} ignored. Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}");
            }
        }

        private void HandlePriceTrendSignal(TrendSignal signal, TrendSignalResult result)
        {
            //Log($"[price-trend]: {result}", LogLevel.Critical, result.SignalTime);
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

            Log($"[rsi-trend]: {result}", LogLevel.Critical, result.SignalTime);

            if (!portfolio.IsEmpty)
            {
                var quantity = portfolio.Quantity < OrderQuantity ? portfolio.Quantity : 0;
                if (quantity > 0)
                {
                    var trend = result.Trend;
                    if (Math.Abs(trend.Change) < MinRsiChange) return;
                    if ((trend.Direction == TrendDirection.ReturnDown || trend.Direction == TrendDirection.MoreUp)  && portfolio.IsLong && portfolio.AvgCost < marketPrice)
                    {
                        if (maSignal != null) maSignal.Reset();
                        sendOrder(Symbol, quantity, BuySell.Sell, $"[{result.Signal.Name}:{trend.Direction}]", 0, OrderIcon.PositionClose, result.SignalTime, result);

                    }
                    else if ((trend.Direction == TrendDirection.ReturnUp || trend.Direction == TrendDirection.LessDown) && portfolio.IsShort && portfolio.AvgCost > marketPrice)
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
                else if (result.Signal.Name == "rsi")
                {
                    var tpSignal = (RangeSignal)(result.Signal);
                    var signalResult = (RangeSignalResult)result;
                    HandleRsiSignal(tpSignal, signalResult);
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
                else HandleBuyOrSellSignal(signal, result);

            }
            finally
            {
                operationWait.Set();
            }
        }


        public void HandleBuyOrSellSignal(Signal signal, SignalResultX signalResult)
        {
            decimal doubleMultiplier = 1.0M;
            BuySell? side = null;
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort) return;

            if (signalResult.finalResult == BuySell.Buy)
            {
                if (!portfolio.IsLong)
                {
                    side = BuySell.Buy;
                    if (this.DoublePositions)
                    {
                        if (portfolio.IsShort)
                        {
                            doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && (ProfitQuantity > 0)) ? 1.5M : 2.0M;
                        }
                    }
                }
            }

            else if (signalResult.finalResult == BuySell.Sell)
            {
                if (!portfolio.IsShort)
                {
                    side = BuySell.Sell;
                    if (this.DoublePositions)
                    {
                        if (portfolio.IsLong)
                        {
                            doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && (ProfitQuantity > 0)) ? 1.5M : 2.0M;
                        }
                    }
                }
            }
            if (side != null)
            {
                sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + signalResult.Signal.Name + "]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            }
        }



        public override string ToString()
        {
            var assembly = typeof(MyAlgo).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }



    }
}
