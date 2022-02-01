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




namespace Kalitte.Trading.Algos
{



    public class MaProfit : AlgoBase
    {
        private ManualResetEvent orderWait = new ManualResetEvent(true);
        private ManualResetEvent operationWait = new ManualResetEvent(true);
        private ManualResetEvent backtestWait = new ManualResetEvent(false);


        //[SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";

        //[Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

        [Parameter(2)]
        public decimal OrderQuantity = 2M;

        [Parameter(5)]
        public int MovPeriod = 5;

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

        [Parameter(1)]
        public decimal LossQuantity = 1;

        [Parameter(9)]
        public decimal ProfitPuan = 9;

        [Parameter(9)]
        public decimal LossPuan = 9;

        //[Parameter(0)]
        public int RsiLong = 0;

        //[Parameter(0)]
        public int RsiShort = 0;

        [Parameter(0)]
        public int MACDLongPeriod = 0;

        [Parameter(5)]
        public int MACDShortPeriod = 5;

        [Parameter(3)]
        public int MACDTrigger = 3;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;

        public string logDir = @"c:\kalitte\log";
        private MarketDataFileLogger fileLogger;


        [Parameter(false)]
        public bool BackTestMode = false;

        int virtualOrderCounter = 0;

        decimal takeProfitTotal = 0;
        decimal stopLossTotal = 0;




        PortfolioList portfolios = new PortfolioList();
        ExchangeOrder positionRequest = null;
        System.Timers.Timer orderTimer;




        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);


            if (!SimulateOrderSignal && MovPeriod > 0) this.signals.Add(new CrossSignal("MA59Cross", this, !SimulateOrderSignal && MovPeriod > 0, mov, mov2));
            if (!SimulateOrderSignal && MACDLongPeriod > 0) this.signals.Add(new CrossSignal("MACD59Cross", this, !SimulateOrderSignal && MACDLongPeriod > 0, macd, macd.MacdTrigger));
            if (SimulateOrderSignal) this.signals.Add(new FlipFlopSignal("FlipFlop", this, SimulateOrderSignal, OrderSide.Buy));

            signals.ForEach(p => p.TimerEnabled = !BackTestMode);
            signals.ForEach(p => p.Simulation = BackTestMode);
            signals.ForEach(p => p.OnSignal += SignalReceieved);

            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            orderTimer = new System.Timers.Timer(3000);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !BackTestMode)
            {
                AddSymbolMarketData(Symbol);
                SetTimerInterval(1);
            }
            this.fileLogger = new MarketDataFileLogger(Symbol, logDir, "price");
        }



        public decimal GetMarketPrice(DateTime? t = null)
        {
            if (BackTestMode) return fileLogger.GetMarketData(t.HasValue ? t.Value : DateTime.Now);
            var price = this.GetMarketData(Symbol, SymbolUpdateField.Last);
            return price;
        }

        public override void OnRealPositionUpdate(AlgoTraderPosition position)
        {

            //if (position.Symbol == Symbol)
            //{
            //	lock (ordrLock)
            //	{
            //		portfolios.UpdateFromTrade(position);
            //		Log("Portfolio Updated");
            //		Log(portfolios.Print());
            //		Log(portfolios.Print());
            //	}
            //}


            //Log($"sym: {position.Symbol} side:{position.Side} total:{position.TotalPosition} amount:{position.Amount} cost:{position.AvgCost} avail:{position.QtyAvailable} net:{position.QtyNet}");

        }

        public void LoadRealPositions()
        {
            var positions = BackTestMode ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            portfolios.LoadRealPositions(positions, p => p.Symbol == this.Symbol);
            Log($"- PORTFOLIO -");
            Log($"{portfolios.Print()}");
        }




        public void startBackTest()
        {
            var start = new DateTime(2022, 01, 28, 9, 30, 0);
            var end = new DateTime(2022, 01, 28, 18, 0, 0);

            var seconds = (end - start).TotalSeconds;

            for(var i = 0; i < seconds; i++)
            {
                var t = start.AddSeconds(i);
                //            var result = this.ManageProfitLoss(price, date);
                //            if (result.HasValue) break;
            }



            //var portfolio = portfolios.GetPortfolio(Symbol);
            //if (ProfitQuantity > 0 && !portfolio.IsEmpty)
            //{
            //    var t = barDataCurrentValues.LastUpdate.DTime;
            //    var periodsToBack = GetSymbolPeriodSeconds(SymbolPeriod);

            //    var start = t - TimeSpan.FromSeconds(periodsToBack - 1);
            //    Log($"Backtest starting to check take profit. Bar: {t}, start: {start}  seconds back: {periodsToBack}", LogLevel.Debug);
            //    for (var i = 0; i < periodsToBack; i++)
            //    {
            //        var date = start.AddSeconds(i);
            //        var price = GetMarketPrice(date);
            //        if (price != 0)
            //        {                        
            //            var result = this.ManageProfitLoss(price, date);
            //            if (result.HasValue) break;
            //        }
            //    }
            //}

            //foreach (var signal in signals)
            //{
            //    var result = signal.Check();
            //    SignalReceieved(signal, new SignalEventArgs() { Result = result });
            //}
            //Decide();

            backtestWait.Set();
        }


        public override void OnInitCompleted()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            Log($"Inited with {assembly.FullName}");
            LoadRealPositions();            
            signals.ForEach(p => p.Start());
            if (!BackTestMode)
            {
                orderTimer.Elapsed += OnOrderTimerEvent;
                orderTimer.Enabled = true;
            }            
        }

        private void OnOrderTimerEvent(Object source, ElapsedEventArgs e)
        {
            if (BackTestMode) return;
            orderTimer.Enabled = false;
            try
            {
                Decide();
            }
            finally
            {
                orderTimer.Enabled = true;
            }
        }

        private void SignalReceieved(Signal signal, SignalEventArgs data)
        {
            lock (signalResults)
            {
                if (data.Result.finalResult.HasValue) Log($"Signal received from {signal.Name} as {data.Result.finalResult }", LogLevel.Debug);
                signalResults[signal.Name] = data.Result;
            }
        }


        private void Decide()
        {
            lock (signalResults)
            {
                Log($"Deciding with {signalResults.Count} results from {signals.Count} signals.", LogLevel.Debug);
                var result = signalResults.Where(p => p.Value.finalResult.HasValue).FirstOrDefault().Value;
                if (result != null)
                {
                    operationWait.WaitOne();
                    orderWait.WaitOne();
                    operationWait.Reset();
                    try
                    {
                        Log($"Decided signal as {result.finalResult} from {result.Signal.Name}", LogLevel.Debug);
                        CreateOrders(result);
                    }
                    finally
                    {
                        operationWait.Set();
                    }
                }
                else Log("Bot decided", LogLevel.Debug);
            }
        }

        public void CreateOrders(SignalResultX signalResult)
        {
            decimal doubleMultiplier = 1.0M;
            OrderSide? side = null;
            var portfolio = this.portfolios.GetPortfolio(Symbol);

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
                sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + signalResult.Signal.Name + "]");
            }


        }



        //public bool ensureWaitingPositions()
        //{
        //    if (this.positionRequest != null)
        //    {
        //        //Log($"active position waiting: {positionRequest.Id}/{positionRequest.Symbol}/{positionRequest.Side}/{positionRequest.Quantity}");

        //        return false;
        //    }
        //    else return true;

        //}

        

        public OrderSide? ManageProfitLoss(decimal? marketPrice = null, DateTime? t = null)
        {
            //if (!this.ensureWaitingPositions())
            //{
            //    Log($"waiting orders: {this.positionRequest.Id}");
            //    return null;
            //}
            operationWait.WaitOne();
            orderWait.WaitOne();
            operationWait.Reset();
            OrderSide? result = null;
            try
            {
                var portfolio = portfolios.GetPortfolio(Symbol);

                if (!portfolio.IsEmpty)
                {
                    var price = marketPrice.HasValue ? marketPrice.Value : GetMarketPrice(t);
                    var pl = price - portfolio.AvgCost;

                    if (price == 0)
                    {
                        Log($"ProfitLoss price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                    }
                    else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Buy) && (pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        takeProfitTotal += Math.Abs(pl);
                        Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, ProfitQuantity, OrderSide.Sell, $"take profit order, PL: {pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
                        result = OrderSide.Sell;
                    }
                    else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Sell) && (-pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        takeProfitTotal += Math.Abs(pl);
                        Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, ProfitQuantity, OrderSide.Buy, $"take profit order, PL: {-pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
                        result = OrderSide.Buy;
                    }
                    else if ((this.LossQuantity > 0) && (portfolio.Side == OrderSide.Buy) && (pl <= -this.LossPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        stopLossTotal += Math.Abs(pl);
                        Log($"Stoploss: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, LossQuantity, OrderSide.Sell, $"stop loss order, PL: {pl}, stopLossTotal: {stopLossTotal}", price, ChartIcon.StopLoss);
                        result = OrderSide.Sell;
                    }
                    else if ((this.LossQuantity > 0) && (portfolio.Side == OrderSide.Sell) && (pl >= this.LossPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        stopLossTotal += Math.Abs(pl);
                        Log($"Stoploss: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, LossQuantity, OrderSide.Buy, $"stop loss order, PL: {pl}, stopLossTotal: {stopLossTotal}", price, ChartIcon.StopLoss);
                        result = OrderSide.Buy;
                    }
                }
            }
            finally
            {
                operationWait.Set();
            }



            return result;

        }

        public override void OnTimer()
        {
            ManageProfitLoss();
        }



        protected ExchangeOrder sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0, ChartIcon icon = ChartIcon.None)
        {
            //if (!this.ensureWaitingPositions())
            //{
            //    Log("Bekleyen pozisyon varken yeni pozisyon gönderilemez");
            //    return null;
            //}
            Log($"Order received: {symbol} {quantity} {side} {comment}", LogLevel.Debug);
            orderWait.Reset();
            var price = lprice > 0 ? lprice : GetMarketPrice();
            string orderid;
            decimal limitPrice = Math.Round((price + price * 0.02M * (side == OrderSide.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;


            if (UseVirtualOrders)
            {
                orderid = virtualOrderCounter++.ToString();
            }
            else
            {
                orderid = BackTestMode ? this.SendMarketOrder(symbol, quantity, side, icon) :
                this.SendLimitOrder(symbol, quantity, side, limitPrice, icon, DateTime.Now.Hour >= 19);
            }

            this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment);
            Log($"Order created, waiting to complete: {this.positionRequest.ToString()}");
            if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
            return this.positionRequest;
        }



        DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            if (!this.BackTestMode) return;



            var portfolio = portfolios.GetPortfolio(Symbol);
            if (ProfitQuantity > 0 && !portfolio.IsEmpty)
            {
                var t = barDataCurrentValues.LastUpdate.DTime;
                var periodsToBack = GetSymbolPeriodSeconds(SymbolPeriod);

                var start = t - TimeSpan.FromSeconds(periodsToBack - 1);
                Log($"Backtest starting to check take profit. Bar: {t}, start: {start}  seconds back: {periodsToBack}", LogLevel.Debug);
                for (var i = 0; i < periodsToBack; i++)
                {
                    var date = start.AddSeconds(i);
                    var price = GetMarketPrice(date);
                    if (price != 0)
                    {
                        var result = this.ManageProfitLoss(price, date);
                        if (result.HasValue) break;
                    }
                }
            }

            foreach (var signal in signals)
            {
                var result = signal.Check();
                SignalReceieved(signal, new SignalEventArgs() { Result = result });
            }
            Decide();
        }


        public void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            this.positionRequest.FilledUnitPrice = filledUnitPrice;
            this.positionRequest.FilledQuantity = filledQuantity;
            var portfolio = this.portfolios.Add(this.positionRequest);
            Log($"Completed order: {this.positionRequest.ToString()}");
            Log($"Portfolio: {portfolio.ToString()}");
            this.positionRequest = null;
            orderWait.Set();
        }

        public override void OnOrderUpdate(IOrder order)
        {
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                //Log($"fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}");
                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    if (BackTestMode)
                    {
                        this.FillCurrentOrder(order.Price, this.positionRequest.Quantity);
                    }
                    else
                    {
                        this.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                    }
                }
            }
        }



        public override void OnStopped()
        {
            orderTimer.Stop();
            signals.ForEach(p => p.Stop());
            Log($"Portfolio ended: {portfolios.Print()}");
        }
    }

}

