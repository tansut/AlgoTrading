// algo
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Kalitte.Trading.Algos
{
    public class MyAlgo : AlgoBase
    {

        public PowerSignalResult LastPower { get; set; } = null;


        [AlgoParam(4)]
        public decimal OrderQuantity { get; set; } = 4M;


        [AlgoParam(2)]
        public int CrossPriceCollectionPeriod { get; set; } = 2;




        [AlgoParam(true)]
        public bool UseSmaForCross { get; set; } = true;

        [AlgoParam(5)]
        public int MovPeriod { get; set; } = 5;

        [AlgoParam(9)]
        public int MovPeriod2 { get; set; } = 9;

        [AlgoParam(false)]
        public bool DynamicCross { get; set; } = false;


        [AlgoParam(0.25)]
        public decimal MaAvgChange { get; set; } = 0.25M;

        [AlgoParam(15)]
        public int MaPeriods { get; set; } = 25;


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

        [AlgoParam(1)]
        public decimal RsiProfitPuan { get; set; } = 1M;



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

        [AlgoParam(9)]
        public int PowerLookback { get; set; } = 60;

        //[AlgoParam(9)]
        //public int PowerBarSeconds { get; set; } = 60;

        [AlgoParam(9)]
        public int PowerVolumeCollectionPeriod { get; set; } = 15;

        [AlgoParam(0)]
        public int PowerCrossThreshold { get; set; } = 0;

        public FinanceBars MinBars = null;

        CrossSignal maSignal = null;
        CrossSignal macSignal = null;
        TakeProfitOrLossSignal takeProfitSignal = null;
        TrendSignal rsiTrendSignal = null;
        TrendSignal maTrendSignal = null;
        TrendSignal priceTrend = null;
        TrendSignal atrTrend = null;
        PowerSignal powerSignal = null;

        

        public override void InitMySignals(DateTime t)
        {

            var periodData = GetSymbolData(this.Symbol, this.SymbolPeriod);
            var oneMinData = GetSymbolData(this.Symbol, BarPeriod.Min);
            var price = new Price(periodData.Periods, 9);
            priceTrend.i1k = price;

            var atr = new Atrp(oneMinData.Periods, 2);
            atrTrend.i1k = atr;

            //var power = new Volume(periodData.Periods, PowerLookback, PowerBarSeconds);
            //
            powerSignal.Indicator = new Ema(oneMinData.Periods, PowerLookback);
            powerSignal.VolumeCollectionPeriod = PowerVolumeCollectionPeriod;

            if (maSignal != null)
            {
                //maSignal.i1k = new Ema(PeriodBars, MovPeriod);
                //maSignal.i2k = new Ema(PeriodBars, MovPeriod2);
                maSignal.i1k = new Macd(periodData.Periods, MovPeriod, MovPeriod2, MACDTrigger);
                maSignal.i2k = new Custom((q) => 0, periodData.Periods, MovPeriod + MovPeriod2 + MACDTrigger);
               


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
            base.InitializeBars(symbol, BarPeriod.Min, t);
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

            this.powerSignal = new PowerSignal("power", Symbol, this);

            this.Signals.Add(this.priceTrend);
            this.Signals.Add(this.atrTrend);
            this.Signals.Add(this.powerSignal);


            if (MovPeriod > 0 && !SimulateOrderSignal)
            {
                this.maSignal = new CrossSignal("cross:ma59", Symbol, this) { UseSma = UseSmaForCross, PriceCollectionPeriod = CrossPriceCollectionPeriod, AvgChange = MaAvgChange, Periods = MaPeriods };
                this.Signals.Add(maSignal);
            }

            if (MACDShortPeriod > 0 && !SimulateOrderSignal)
            {
                this.macSignal = new CrossSignal("cross:macd593", Symbol, this) { UseSma = UseSmaForCross, PriceCollectionPeriod = CrossPriceCollectionPeriod, AvgChange = MacdAvgChange, Periods = MacdPeriods };
                this.Signals.Add(macSignal);
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
            //this.AddSymbol(this.Symbol)
            InitSignals();
            base.Init();
        }

        public override void Stop()
        {
            base.Stop();
            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;

            if (Simulation && ExpectedNetPl != 0 && netPL < ExpectedNetPl) File.Delete(LogFile);
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

        void HandlePowerSignal(PowerSignal signal, PowerSignalResult result)
        {

            if ((LastPower == null || LastPower.Power != result.Power) && result.Power != PowerRatio.Unknown)
            {
                var last = LastPower != null ? LastPower.Power.ToString() : "";
                //Log($"Power changed from {last} -> {result.Power}. Signal: {result} ", LogLevel.Warning, result.SignalTime);
                LastPower = result;
            }

            if (DynamicCross && result.Power != PowerRatio.Unknown)
            {
                double ratio = 0;
                if (result.Value < PowerCrossThreshold || PowerCrossThreshold == 0)
                {
                    ratio = 0.25 * (double)(100 - result.Value) / 100D;
                }
                //if (ratio < 0) ratio = 0;
                //ratio = (double)(100 - result.Value) / 100D;
                Signals.Where(p => p is CrossSignal).Select(p => (CrossSignal)p).ToList().ForEach(p => p.AdjustSensitivity(ratio, $"{result.Power}/{result.Value}"));
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


                    //Log($"Current ATR volatility level: {result}", LogLevel.Warning);
                    //Log($"Current ATR volatility level: {VolatileRatio}. Adjusted cross to {maSignal.AvgChange}, {maSignal.Periods}. Signal: {result}", LogLevel.Critical, result.SignalTime);
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
                var quantity = portfolio.Quantity <= OrderQuantity && portfolio.Quantity > RsiProfitQuantity ? Math.Min(portfolio.Quantity, RsiProfitQuantity) : 0; // : RsiProfitQuantity;
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



        public void HandleCrossSignal(CrossSignal signal, CrossSignalResult signalResult)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (signalResult.finalResult == BuySell.Buy && portfolio.IsLong) return;
            if (signalResult.finalResult == BuySell.Sell && portfolio.IsShort) return;

            var orderQuantity = portfolio.Quantity + OrderQuantity;

            var cross = (CrossSignal)signalResult.Signal;
            sendOrder(Symbol, orderQuantity, signalResult.finalResult.Value, $"[{signalResult.Signal.Name}/{cross.AvgChange},{cross.Periods}]", 0, OrderIcon.None, signalResult.SignalTime, signalResult);
            //signal.AdjustSensitivity(0.30, "Order Received");
        }

        public override void sendOrder(string symbol, decimal quantity, BuySell side, string comment = "", decimal lprice = 0, OrderIcon icon = OrderIcon.None, DateTime? t = null, SignalResultX signalResult = null)
        {
            base.sendOrder(symbol, quantity, side, comment, lprice, icon, t, signalResult);
            //var sep = ",";
            //Log($"ATR: {string.Join(sep, atrTrend.i1k.Results.Select(p => p.Value))}", LogLevel.Critical);
            Log($"ART: {atrTrend.i1k.Results.Last().Date} {atrTrend.i1k.Results.Last().Value}", LogLevel.Critical);
            Log($"Power: {LastPower}", LogLevel.Critical);
        }


        public override string ToString()
        {
            var assembly = typeof(MyAlgo).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }



    }
}
