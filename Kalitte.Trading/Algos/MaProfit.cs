// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Kalitte.Trading.Algos
{



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

        [Parameter(0)]
        public int MovPeriod = 0;

        [Parameter(9)]
        public int MovPeriod2 = 9;

        //[Parameter(true)]
        public bool DoublePositions = true;


        [Parameter(false)]
        public bool UseVirtualOrders = false;

        [Parameter(false)]
        public bool AutoCompleteOrders = false;

        [Parameter(false)]
        public bool SimulateOrderSignal = false;

        [Parameter(1)]
        public decimal ProfitQuantity = 1;

        [Parameter(0)]
        public decimal LossQuantity = 0;

        [Parameter(9)]
        public decimal ProfitPuan = 9;

        [Parameter(9)]
        public decimal LossPuan = 9;

        [Parameter(0)]
        public int RsiLong = 0;

        [Parameter(0)]
        public int RsiShort = 0;

        [Parameter(5)]
        public int MACDShortPeriod = 5;

        [Parameter(9)]
        public int MACDLongPeriod = 9;



        [Parameter(3)]
        public int MACDTrigger = 3;

        [Parameter(false)]
        public bool AlwaysGetProfit = false;

        [Parameter(false)]
        public bool AlwaysStopLoss = false;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;

        decimal simulationPriceDif = 0;


        int virtualOrderCounter = 0;
        ExchangeOrder positionRequest = null;
        System.Timers.Timer orderTimer;


        public override void OnInit()
        {
            AddSymbol(Symbol, Simulation ? SymbolPeriod.Min : SymbolPeriod);
            //AddSymbol(Symbol,  SymbolPeriod);
            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);


            if (!SimulateOrderSignal && MovPeriod > 0) this.signals.Add(new CrossSignal("cross:ma59", Symbol, this, mov, mov2) {  AvgChange = 0.1M, Periods = 8});
            if (!SimulateOrderSignal && MACDShortPeriod > 0) this.signals.Add(new CrossSignal("cross:macd593", Symbol, this, macd, macd.MacdTrigger) { AvgChange = 0.020M, Periods = 8 });
            if (SimulateOrderSignal) this.signals.Add(new FlipFlopSignal("flipflop", Symbol, this, OrderSide.Buy));
            if (!SimulateOrderSignal && this.ProfitQuantity > 0 || this.LossQuantity > 0) this.signals.Add(new TakeProfitOrLossSignal("profitOrLoss", Symbol, this, this.ProfitPuan, this.ProfitQuantity, this.LossPuan, this.LossQuantity));
            if (!SimulateOrderSignal && RsiLong > 0 || RsiShort > 0) this.signals.Add(new RangeSignal("rsi", Symbol, this, rsi, RsiShort == 0 ? new decimal?() : RsiShort, RsiLong == 0 ? new decimal() : RsiLong) { Periods = 1});
            
            signals.ForEach(p =>
            {
                p.TimerEnabled = !Simulation;
                p.Simulation = Simulation;
                p.OnSignal += SignalReceieved;
            });
            
            

            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            orderTimer = new System.Timers.Timer(500);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !Simulation)
            {
                AddSymbolMarketData(Symbol);
                //SetTimerInterval(1);
            }
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
        }


        public override void OnInitCompleted()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            var ma = mov.Period > 0 ? $"Ma[{MovPeriod}/{MovPeriod2}]":"";
            var macd = MACDShortPeriod > 0 ? $"Macd[{MACDShortPeriod}/{MACDLongPeriod}]":"";
            Log($"Inited instance {InstanceName} [{this.Symbol}, {ma} {macd}] using assemly {assembly.FullName}");
            LoadRealPositions(this.Symbol);
            signals.ForEach(p => p.Start());
            if (!Simulation)
            {
                //orderTimer.Elapsed += OnOrderTimerEvent;
                //orderTimer.Enabled = true;
            } 
        }

        private void OnOrderTimerEvent(Object source, ElapsedEventArgs e)
        {
            //if (BackTestMode) return;
            //orderTimer.Enabled = false;
            //try
            //{
            //    Decide();
            //}
            //finally
            //{
            //    orderTimer.Enabled = true;
            //}
        }

        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            if (!this.Simulation) return;

            //var test = new DateTime(2022, 02, 4, 16, 00, 0);

            //if (barDataCurrentValues.LastUpdate.DTime < test) return;
      
            foreach (var signal in signals)
            {
                var t = barDataCurrentValues.LastUpdate.DTime;
                //Log($"Checking signal {signal.Name} for {t}", LogLevel.Debug, t);
                var result = signal.Check(t);
                var waitOthers = waitForOperationAndOrders("Backtest");
            }
        }

        private Boolean waitForOperationAndOrders(string message)
        {
            //Log($"Operations/orders waiting: { message}", LogLevel.Debug);

            var wait = Simulation ? 0 : 100;
            var result1 = operationWait.WaitOne(wait);
            var result2 = orderWait.WaitOne(wait);
            if (!result1) Log($"Waiting for last operation to complete: {message}", LogLevel.Warning);
            if (!result2) Log($"Waitng for last order to complete: {message}", LogLevel.Warning);
            return result1 && result2;
        }

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
                sendOrder(Symbol, result.Direction == ProfitOrLoss.Profit ? signal.ProfitQuantity : signal.LossQuantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? ChartIcon.TakeProfit : ChartIcon.StopLoss, result.SignalTime);
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
                Log($"{signal.Name} couldnot be executed since market price is zero",LogLevel.Debug, result.SignalTime);
            }

            if (!portfolio.IsEmpty)
            {
                Log($"[{result.Signal.Name}:{result.Status}] received.", LogLevel.Debug, result.SignalTime);
                if (portfolio.Side == OrderSide.Sell && result.Status == RangeStatus.BelowMin && portfolio.AvgCost > marketPrice)
                {
                    Log($"{signal.Name} simulate position close,  buy.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    //sendOrder(Symbol, portfolio.Quantity, OrderSide.Buy, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime);
                }
                else if (portfolio.Side == OrderSide.Buy && result.Status == RangeStatus.AboveHigh && portfolio.AvgCost < marketPrice)
                {
                    Log($"{signal.Name} simulate position close,  sell.  Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}", LogLevel.Debug, result.SignalTime);
                    //sendOrder(Symbol, portfolio.Quantity, OrderSide.Sell, $"[{result.Signal.Name}:{result.Status}]", 0, ChartIcon.PositionClose, result.SignalTime);
                }
                else Log($"{signal.Name} ignored. Rsi: {signal.Indicator.CurrentValue}, Market price: {marketPrice}, {portfolio.ToString()}");
            }
        }

        private void Decide(Signal signal, SignalEventArgs data)
        {
            var result = data.Result;
            //Log($"Deciding with {signalResults.Count} results from {signals.Count} signals.", LogLevel.Debug);
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
                } else if (result.Signal.Name == "rsi")
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
                sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + signalResult.Signal.Name + "]", 0, ChartIcon.None, signalResult.SignalTime);
            }
        }



        protected void sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0, ChartIcon icon = ChartIcon.None, DateTime? t = null)
        {
            orderWait.Reset();
            var price = lprice > 0 ? lprice : GetMarketPrice(this.Symbol, t);
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
            this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment, t);
            Log($"Order created, waiting to complete. Market price was: {price}: {this.positionRequest.ToString()}", LogLevel.Info, t);
            if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
        }



        DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }




        public void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            this.positionRequest.FilledUnitPrice = filledUnitPrice;
            this.positionRequest.FilledQuantity = filledQuantity;
            var portfolio = this.UserPortfolioList.Add(this.positionRequest);
            Log($"Completed order: {this.positionRequest.ToString()}\n{printPortfolio()}", LogLevel.Info, positionRequest.Time);            
            this.positionRequest = null;
            orderWait.Set();
        }



        public override void OnOrderUpdate(IOrder order)
        {
            Log($"OrderUpdate: status: {order.OrdStatus.Obj} orderid: {order.CliOrdID} fa: {order.FilledAmount}"); 
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                //if (!BackTestMode) Log($"OrderUpdate: pos: {this.positionRequest} status: {order.OrdStatus.Obj} orderid: {order.CliOrdID} fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}", LogLevel.Debug, this.positionRequest != null ? this.positionRequest.Time: DateTime.Now);

                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    if (Simulation)
                    {
                        if (positionRequest.UnitPrice > 0)
                        {
                            var gain = ((order.Price - positionRequest.UnitPrice) * (positionRequest.Side == OrderSide.Buy ? 1 : -1) * positionRequest.Quantity);
                            simulationPriceDif += gain;
                            Log($"Collected market price is {positionRequest.UnitPrice}, backtest market price: {order.Price} [{gain}]", LogLevel.Warning, positionRequest.Time);
                            //this.FillCurrentOrder(positionRequest.UnitPrice, this.positionRequest.Quantity);
                        }
                        this.FillCurrentOrder(order.Price, this.positionRequest.Quantity);
                    } else if (UseVirtualOrders)
                    {
                        this.FillCurrentOrder(positionRequest.UnitPrice, this.positionRequest.Quantity);
                    }
                    else
                    {
                        this.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                    }
                }
            } else if (order.OrdStatus.Obj == OrdStatus.Rejected || order.OrdStatus.Obj == OrdStatus.Canceled)
            {
                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    CancelCurrentOrder(order);
                }                    
            }
        }

        private void CancelCurrentOrder(IOrder order)
        {
            Log($"Order rejected/cancelled [{order.OrdStatus.Obj}]", LogLevel.Warning, this.positionRequest.Time);
            this.positionRequest = null;
            orderWait.Set();
        }

        public string printPortfolio()
        {
            var portfolio = UserPortfolioList.Print();
            if (Simulation && portfolio.Length > 0)
            {
                portfolio.Append($"Market price difference: [{ simulationPriceDif}] Expected: [PL: {simulationPriceDif+UserPortfolioList.PL} NetPL: {simulationPriceDif + UserPortfolioList.PL-UserPortfolioList.Comission}]");
            }
            return "-- RECENT PORTFOLIO --" + Environment.NewLine + portfolio.ToString() + Environment.NewLine + "-- END PORTFOLIO --" ;
        }

        public override void OnStopped()
        {
            orderTimer.Stop();
            signals.ForEach(p => p.Stop());
            Log($"Completed Algo.\n {printPortfolio()}");
            if (Simulation) Process.Start(LogFile);
        }
    }

}

