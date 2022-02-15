// algo
using Kalitte.Trading.Indicators;
using Matriks.Data.Symbol;
using Matriks.Indicators;
using Matriks.Lean.Algotrader.Models;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Trader.Core.TraderModels;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading.Matrix
{

    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }


    public class MaProfit : AlgoBase
    {
        private ManualResetEvent orderWait = new ManualResetEvent(true);
        private ManualResetEvent operationWait = new ManualResetEvent(true);
        private ManualResetEvent backtestWait = new ManualResetEvent(true);


        //[SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";


        [Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

        [Parameter(2)]
        public decimal OrderQuantity = 2M;


        [Parameter(1)]
        public int PriceCollectionPeriod = 1;

        [Parameter(false)]
        public bool UseSmaForCross = false;

        [Parameter(5)]
        public int MovPeriod = 5;

        [Parameter(9)]
        public int MovPeriod2 = 9;


        [Parameter(0.15)]
        public decimal MaAvgChange = 0.15M;

        [Parameter(25)]
        public int MaPeriods = 25;

        [Parameter(2)]
        public int CrossNextOrderMultiplier = 2;

        

        [Parameter(0)]
        public decimal ExpectedNetPl = 0;


        //[Parameter(true)]
        public bool DoublePositions = true;


        [Parameter(true)]
        public bool UseVirtualOrders = true;

        //[Parameter(false)]
        public bool AutoCompleteOrders = false;

        //[Parameter(false)]
        public bool SimulateOrderSignal = false;

        [Parameter(1)]
        public decimal ProfitQuantity = 1;

        [Parameter(0)]
        public decimal LossQuantity = 0;

        [Parameter(16)]
        public decimal ProfitPuan = 16;

        [Parameter(4)]
        public decimal LossPuan = 4;

        [Parameter(0)]
        public int RsiHighLimit = 0;

        [Parameter(0)]
        public int RsiLowLimit = 0;

        [Parameter(9)]
        public int Rsi = 9;

        [Parameter(30)]
        public int RsiAnalysisPeriod = 30;

        [Parameter(0)]
        public int MACDShortPeriod = 0;

        [Parameter(9)]
        public int MACDLongPeriod = 9;

        [Parameter(0.05)]
        public decimal MacdAvgChange = 0.05M;

        [Parameter(15)]
        public int MacdPeriods = 15;

        [Parameter(6)]
        public int MACDTrigger = 6;

        [Parameter(false)]
        public bool AlwaysGetProfit = false;

        [Parameter(false)]
        public bool AlwaysStopLoss = false;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;


        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        FinanceBars periodBars = null;
        //FinanceBars priceBars = null;

        decimal simulationPriceDif = 0;
        int orderCounter = 0;

        public Dictionary<string, decimal> ordersBySignals = new Dictionary<string, decimal>();


        int virtualOrderCounter = 0;
        ExchangeOrder positionRequest = null;
        int simulationCount = 0;
        public StartableState SignalsState { get; private set; } = StartableState.Stopped;

        private void CheckDelayedOrders(DateTime t)
        {
            if (this.delayedOrder != null)
            {
                var dif = AlgoTime - delayedOrder.scheduled2;
                if (dif.Seconds >= 0)
                {
                    Log($"Simulation completed at {t}  for {delayedOrder.order.Id}", LogLevel.Debug);
                    FillCurrentOrder(delayedOrder.order.UnitPrice, delayedOrder.order.Quantity);
                    this.delayedOrder = null;
                }
            }
        }

        public void CountOrder(string signal, decimal quantity)
        {
            lock (ordersBySignals)
            {
                decimal existing;
                if (ordersBySignals.TryGetValue(signal, out existing))
                {
                    ordersBySignals[signal] = existing + quantity;
                }
                else ordersBySignals[signal] = quantity;
            }
        }

        public void StartSignals()
        {
            SignalsState = StartableState.StartInProgress;
            try
            {
                foreach (var signal in signals)
                {
                    signal.Start();
                    Log($"Started signal {signal}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}", LogLevel.Error);
            }

            SignalsState = StartableState.Started; ;
        }

        public void StopSignals()
        {
            SignalsState = StartableState.StopInProgress;
            try
            {
                foreach (var signal in signals)
                {

                    signal.Stop();
                    Log($"Stopped signal {signal}", LogLevel.Info);

                }
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}", LogLevel.Error);
            }

            SignalsState = StartableState.Stopped;


        }



        public void InitMySignals(DateTime t)
        {

            if (periodBars == null)
            {
                periodBars = new FinanceBars();
                try
                {
                    //var bd = GetBarData(Symbol, SymbolPeriod);
                    //for (var i = 0; i < bd.BarDataIndexer.LastBarIndex; i++)
                    //{
                    //    if (bd.BarDataIndexer[i] > t) break;
                    //    var quote = new MyQuote() { Date = bd.BarDataIndexer[i], Open = bd.Open[i], High = bd.High[i], Low = bd.Low[i], Close = bd.Close[i], Volume = bd.Volume[i] };
                    //    periodBars.Push(quote);
                    //}
                    var mdp = new MarketDataFileLogger(Symbol, LogDir, SymbolPeriod.ToString());
                    mdp.FileName = "all.txt";
                    mdp.SaveDaily = true;
                    periodBars = mdp.GetContentAsQuote(t);
                    Log($"Initialized total {periodBars.Count} using time {t}. Last bar is: {periodBars.Last}", LogLevel.Debug, t);
                } catch(Exception ex)
                {
                    Log($"Error initializing bars {ex.Message}", LogLevel.Error, t);
                }
            }


            var ma = signals.Where(p => p.Name == "cross:ma59").FirstOrDefault() as CrossSignal;
            if (ma != null)
            {
                var movema5 = new Ema(periodBars, MovPeriod);
                var mov2ema9 = new Ema(periodBars, MovPeriod2);
                ma.i1k = movema5;
                ma.i2k = mov2ema9;
            }


            var macds = signals.Where(p => p.Name == "cross:macd593").FirstOrDefault() as CrossSignal;

            if (macds != null)
            {

                var macdi = new Macd(periodBars, MACDShortPeriod, MACDLongPeriod, MACDTrigger);

                macds.i1k = macdi;
                macds.i2k = macdi.Trigger;
            }


            var rsiRange = signals.Where(p => p.Name == "rsi").FirstOrDefault() as RangeSignal;

            if (rsiRange != null)
            {
                var rsi = new Rsi(periodBars, Rsi);
                rsiRange.i1k = rsi;
            }

            signals.ForEach(p =>
            {
                p.Init();
            });

        }

        public void InitSignals()
        {
            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);


            if (MovPeriod > 0 && !SimulateOrderSignal)
            {
                var ma = new CrossSignal("cross:ma59", Symbol, this, mov, mov2) { UseSma = UseSmaForCross, UseZeroZone =false, PriceCollectionPeriod= PriceCollectionPeriod, NextOrderMultiplier = CrossNextOrderMultiplier, AvgChange = MaAvgChange, Periods = MaPeriods };
                
                this.signals.Add(ma);
            }

            if (MACDShortPeriod > 0 && !SimulateOrderSignal)
            {
                var macds = new CrossSignal("cross:macd593", Symbol, this, macd, macd.MacdTrigger) { UseSma = UseSmaForCross, UseZeroZone =true, PriceCollectionPeriod= PriceCollectionPeriod, NextOrderMultiplier = CrossNextOrderMultiplier, AvgChange = MacdAvgChange, Periods = MacdPeriods };
                this.signals.Add(macds);
            }

            if (SimulateOrderSignal) this.signals.Add(new FlipFlopSignal("flipflop", Symbol, this, OrderSide.Buy));
            if (!SimulateOrderSignal && (this.ProfitQuantity > 0 || this.LossQuantity > 0)) this.signals.Add(new TakeProfitOrLossSignal("profitOrLoss", Symbol, this, this.ProfitPuan, this.ProfitQuantity, this.LossPuan, this.LossQuantity));
            if (!SimulateOrderSignal && (RsiHighLimit > 0 || RsiLowLimit > 0)) this.signals.Add(new RangeSignal("rsi", Symbol, this, rsi, RsiLowLimit == 0 ? new decimal?() : RsiLowLimit, RsiHighLimit == 0 ? new decimal() : RsiHighLimit) { AnalysisPeriod = RsiAnalysisPeriod });

            signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
            });
        }

        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !Simulation)
            {
                AddSymbolMarketData(Symbol);
            }
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
            InitSignals();            

        }

        private void SeansTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 5);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 18, 30, 0);
            var t3 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 5);
            var t4 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);

            seansTimer.Enabled = false;
            try
            {
                if ((t >= t1 && t <= t2) || (t >= t3 && t <= t4))
                {
                    if (SignalsState == StartableState.Stopped)
                    {
                        Log($"Time seems OK, starting signals ...");
                        StartSignals();
                    }
                }
                else
                {
                    if (SignalsState == StartableState.Started)
                    {
                        Log($"Time seems OK, stopping signals ...");
                        StopSignals();
                    }
                }
            }
            finally
            {
                seansTimer.Enabled = true;
            }

        }

        public void CompleteInit()
        {
            if (!Simulation)
            {
                Log($"Setting seans timer ...");
                seansTimer = new System.Timers.Timer(1000);
                seansTimer.Enabled = true;
                seansTimer.Elapsed += SeansTimer_Elapsed;
            }
            else StartSignals();
        }

        public override string ToString()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            return $"Instance {InstanceName} [{this.Symbol}/{SymbolPeriod}] using assembly {assembly.FullName}";
        }

        public override void OnInitCompleted()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            Log($"{this}", LogLevel.Info);
            LoadRealPositions(this.Symbol);
            if (!Simulation) InitMySignals(DateTime.Now);
            if (!Simulation) CompleteInit();
        }


        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {

            if (Simulation)
            {

                lock (this)
                {

                    var bd = barDataCurrentValues.LastUpdate;
                    AlgoTime = bd.DTime;
                    var seconds = GetSymbolPeriodSeconds(SymbolPeriod);

                    //if (simulationCount > 12) return;                                       

                    if (SignalsState != StartableState.Started)
                    {
                        var lastDay = new DateTime(AlgoTime.Year, AlgoTime.Month, AlgoTime.Day).AddDays(-1).AddHours(22).AddMinutes(50);
                        InitMySignals(lastDay);
                        CompleteInit();
                    }

                    Log($"Running backtest for period: {periodBars.Last}", LogLevel.Verbose);

                    for (var i = 0; i < seconds; i++)
                    {
                        var time = AlgoTime;
                        foreach (var signal in signals)
                        {
                            var result = signal.Check(time);
                            //var waitOthers = waitForOperationAndOrders("Backtest");
                        }
                        CheckDelayedOrders(time);
                        AlgoTime = AlgoTime.AddSeconds(1);
                    }
                    simulationCount++;

                    var newQuote = new MyQuote() { Date = barDataCurrentValues.LastUpdate.DTime, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                    periodBars.Push(newQuote);
                    Log($"Pushed new bar, last bar is now: {periodBars.Last}", LogLevel.Verbose);
                }



            }
            else
            {
                var bd = GetBarData(Symbol, SymbolPeriod);
                var last = bd.BarDataIndexer.LastBarIndex;
                try
                {
                    var newQuote = new MyQuote() { Date = bd.BarDataIndexer[last], High = bd.High[last], Close = bd.Close[last], Low = bd.Low[last], Open = bd.Open[last], Volume = bd.Volume[last] };
                    periodBars.Push(newQuote);
                    Log($"Pushed new quote, last is now: {periodBars.Last}", LogLevel.Debug);

                }
                catch (Exception ex)
                {
                    Log($"data update: {ex.Message}", LogLevel.Error);
                }
            }
        }



        private Boolean waitForOperationAndOrders(string message)
        {
            var wait = Simulation ? 0 : 100;
            var result1 = operationWait.WaitOne(wait);
            var result2 = orderWait.WaitOne(wait);
            if (!result1 && !Simulation) Log($"Waiting for last operation to complete: {message}", LogLevel.Warning);
            if (!result2 && !Simulation) Log($"Waiting for last order to complete: {message}", LogLevel.Warning);
            return result1 && result2;
        }

        //private Boolean waitForOperationAndOrders(string message)
        //{
        //    var wait = Simulation ? 0 : 100;
        //    var result1 = Simulation && UseVirtualOrders ? operationWait.WaitOne(): operationWait.WaitOne(wait);
        //    var result2 = Simulation && UseVirtualOrders ? orderWait.WaitOne(): orderWait.WaitOne(wait);
        //    if (!result1 && !Simulation) Log($"Waiting for last operation to complete: {message}", LogLevel.Warning);
        //    if (!result2 && !Simulation) Log($"Waitng for last order to complete: {message}", LogLevel.Warning);
        //    return result1 && result2;
        //}

        private void SignalReceieved(Signal signal, SignalEventArgs data)
        {
            SignalResultX existing;
            OrderSide? oldFinalResult = null;
            lock (signalResults)
            {
                if (signalResults.TryGetValue(signal.Name, out existing))
                    oldFinalResult = existing.finalResult;
                signalResults[signal.Name] = data.Result;
            }
            if (oldFinalResult != data.Result.finalResult)
            {
                Log($"Signal {signal.Name} changed from {oldFinalResult} -> {data.Result.finalResult }", LogLevel.Debug, data.Result.SignalTime);
                if (data.Result.finalResult.HasValue) Decide(signal, data);
            }
        }

        private void HandleProfitLossSignal(TakeProfitOrLossSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (result.finalResult == OrderSide.Buy && portfolio.IsLong) return;
            if (result.finalResult == OrderSide.Sell && portfolio.IsShort) return;

            var pq = portfolio.Quantity;

            bool doAction = pq == this.OrderQuantity;

            if (result.Direction == ProfitOrLoss.Profit && AlwaysGetProfit && pq > signal.ProfitQuantity) doAction = true;
            if (result.Direction == ProfitOrLoss.Loss && AlwaysStopLoss && pq > signal.LossQuantity) doAction = true;

            if (doAction)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Debug, result.SignalTime);
                sendOrder(Symbol, result.Direction == ProfitOrLoss.Profit ? signal.ProfitQuantity : signal.LossQuantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? ChartIcon.TakeProfit : ChartIcon.StopLoss, result.SignalTime, result);
            }
            else Log($"[{result.Signal.Name}:{result.Direction}] received but quantity doesnot match. Portfolio: {pq} oq: {this.OrderQuantity}", LogLevel.Debug, result.SignalTime);
        }


        private void HandleRsiSignal(RangeSignal signal, RangeSignalResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (result.finalResult == OrderSide.Buy && portfolio.IsLong) return;
            if (result.finalResult == OrderSide.Sell && portfolio.IsShort) return;

            var pq = portfolio.Quantity;
            var marketPrice = GetMarketPrice(Symbol, result.SignalTime);

            if (marketPrice == 0)
            {
                Log($"{signal.Name} couldnot be executed since market price is zero", LogLevel.Warning, result.SignalTime);
                return;
            }

            if (!portfolio.IsEmpty)
            {
                Log($"[{result.Signal.Name}:{result.Status} {result.Value}] received.", LogLevel.Debug, result.SignalTime);
                if (portfolio.Side == OrderSide.Sell && result.Status == RangeStatus.BelowMin && portfolio.AvgCost > marketPrice)
                {
                    //Log($"{signal.Name} simulate position close,  buy.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    sendOrder(Symbol, portfolio.Quantity, OrderSide.Buy, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime, result);
                }
                else if (portfolio.Side == OrderSide.Buy && result.Status == RangeStatus.AboveHigh && portfolio.AvgCost < marketPrice)
                {
                    //Log($"{signal.Name} simulate position close,  sell.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    sendOrder(Symbol, portfolio.Quantity, OrderSide.Sell, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime, result);
                }
                else Log($"{signal.Name} ignored. Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}");
            }
        }

        private void Decide(Signal signal, SignalEventArgs data)
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
            OrderSide? side = null;
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (signalResult.finalResult == OrderSide.Buy && portfolio.IsLong) return;
            if (signalResult.finalResult == OrderSide.Sell && portfolio.IsShort) return;

            if (signalResult.finalResult == OrderSide.Buy)
            {
                if (!portfolio.IsLong)
                {
                    side = OrderSide.Buy;
                    if (this.DoublePositions)
                    {
                        if (portfolio.IsShort)
                        {
                            doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && (ProfitQuantity > 0)) ? 1.5M : 2.0M;
                        }
                    }
                }
            }

            else if (signalResult.finalResult == OrderSide.Sell)
            {
                if (!portfolio.IsShort)
                {
                    side = OrderSide.Sell;
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
                sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + signalResult.Signal.Name + "]", 0, ChartIcon.None, signalResult.SignalTime, signalResult);
            }
        }



        protected void sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0, ChartIcon icon = ChartIcon.None, DateTime? t = null, SignalResultX signalResult = null)
        {
            orderWait.Reset();
            var price = lprice > 0 ? lprice : GetMarketPrice(this.Symbol, t);
            if (price == 0)
            {
                Log($"Unable to get a marketprice at {t}, using close {periodBars.Last.Close} from {periodBars.Last}", LogLevel.Warning, t);
                price = periodBars.Last.Close;
            }
            string orderid;
            decimal limitPrice = Math.Round((price + price * 0.02M * (side == OrderSide.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;

            if (UseVirtualOrders)
            {
                orderid = virtualOrderCounter++.ToString();
            }
            else
            {
                orderid = Simulation ? this.SendMarketOrder(symbol, quantity, side, icon) :
                this.SendLimitOrder(symbol, quantity, side, limitPrice, icon, DateTime.Now.Hour >= 19);
            }
            var order = this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment, t);
            order.OriginSignal = signalResult;
            order.Sent = t ?? DateTime.Now;

            Log($"Order created, waiting to complete. Market price was: {price}: {this.positionRequest.ToString()}", LogLevel.Info);
            if (this.UseVirtualOrders || this.AutoCompleteOrders)
            {
                if (this.Simulation)
                {
                    var algoTime = AlgoTime;
                    this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(0.5 + new RandomGenerator().NextDouble() * 2) };
                    Log($"Simulating real environment for {delayedOrder.order.Id} time is: {delayedOrder.created}, schedule to: {delayedOrder.scheduled2}", LogLevel.Debug);
                }
                else FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
            }
        }






        public void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            this.positionRequest.FilledUnitPrice = filledUnitPrice;
            this.positionRequest.FilledQuantity = filledQuantity;
            var portfolio = this.UserPortfolioList.Add(this.positionRequest);
            Log($"Completed order {this.positionRequest.Id} created/resulted at {this.positionRequest.Created}/{this.positionRequest.Resulted}: {this.positionRequest.ToString()}\n{printPortfolio()}", LogLevel.Order);
            if (this.positionRequest.OriginSignal != null) CountOrder(this.positionRequest.OriginSignal.Signal.Name, filledQuantity);
            this.positionRequest = null;
            orderCounter++;
            orderWait.Set();
        }



        public override void OnOrderUpdate(IOrder order)
        {
            Log($"OrderUpdate: status: {order.OrdStatus.Obj} cliD: {order.CliOrdID} oid: {order.OrderID} algoid: {order.AlgoId} fa: {order.FilledAmount}", LogLevel.Debug);
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                //if (!BackTestMode) Log($"OrderUpdate: pos: {this.positionRequest} status: {order.OrdStatus.Obj} orderid: {order.CliOrdID} fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}", LogLevel.Debug, this.positionRequest != null ? this.positionRequest.Time: DateTime.Now);

                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    this.positionRequest.Resulted = order.TradeDate;
                    if (Simulation)
                    {
                        if (positionRequest.UnitPrice > 0 && positionRequest.UnitPrice != order.Price)
                        {
                            var gain = ((order.Price - positionRequest.UnitPrice) * (positionRequest.Side == OrderSide.Buy ? 1 : -1) * positionRequest.Quantity);
                            simulationPriceDif += gain;
                            Log($"Filled price difference for order {order.CliOrdID}: Potential: {positionRequest.UnitPrice}, Backtest: {order.Price} Difference: [{gain}]", LogLevel.Warning, positionRequest.Resulted);
                            //this.FillCurrentOrder(positionRequest.UnitPrice, this.positionRequest.Quantity);
                        }
                        this.FillCurrentOrder(order.Price, this.positionRequest.Quantity);
                    }
                    else
                    {
                        this.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                    }
                }
            }
            else if (order.OrdStatus.Obj == OrdStatus.Rejected || order.OrdStatus.Obj == OrdStatus.Canceled)
            {
                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    CancelCurrentOrder(order);
                }
            }  
        }

        private void CancelCurrentOrder(IOrder order)
        {
            Log($"Order rejected/cancelled [{order.OrdStatus.Obj}]", LogLevel.Warning, this.positionRequest.Created);
            this.positionRequest = null;
            orderWait.Set();
        }

        public string printPortfolio()
        {
            var portfolio = UserPortfolioList.Print();
            if (Simulation && portfolio.Length > 0)
            {
                portfolio.Append($"Market price difference: [{ simulationPriceDif}] Expected: [PL: {simulationPriceDif + UserPortfolioList.PL} NetPL: {simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission}]");
            }
            return "-- RECENT PORTFOLIO --" + Environment.NewLine + portfolio.ToString() + Environment.NewLine + "-- END PORTFOLIO --";
        }

        public override void OnStopped()
        {
            if (!Simulation)
            {
                seansTimer.Stop();
                seansTimer.Dispose();
            }
            StopSignals();
            Log($"Completed {this}", LogLevel.FinalResult);
            signals.ForEach(p => Log($"{p}", LogLevel.FinalResult));
            Log($"----------------------", LogLevel.FinalResult);
            Log($"Market price difference total: {this.simulationPriceDif}", LogLevel.FinalResult);
            foreach (var item in ordersBySignals)
            {
                Log($"{item.Key}:{item.Value}", LogLevel.FinalResult);
            }
            Log($"Total orders filled:: {this.orderCounter}", LogLevel.FinalResult);
            Log($"{printPortfolio()}", LogLevel.FinalResult);
            Log($"----------------------", LogLevel.FinalResult);

            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;

            if (Simulation && ExpectedNetPl > 0 && netPL < ExpectedNetPl) File.Delete(LogFile);
            else if (Simulation) Process.Start(LogFile);
            base.OnStopped();
        }
        
    }

}

