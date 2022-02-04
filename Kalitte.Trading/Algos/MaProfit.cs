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
        private ManualResetEvent backtestWait = new ManualResetEvent(true);


        //[SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";

        [Parameter(SymbolPeriod.Min10)]
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




        int virtualOrderCounter = 0;
        ExchangeOrder positionRequest = null;
        System.Timers.Timer orderTimer;


        public override void OnInit()
        {
            AddSymbol(Symbol, BackTestMode ? SymbolPeriod.Min : SymbolPeriod);
            //AddSymbol(Symbol,  SymbolPeriod);
            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);


            if (!SimulateOrderSignal && MovPeriod > 0) this.signals.Add(new CrossSignal("cross:ma59", Symbol, this, mov, mov2) {  AvgChange = 0.1M, Periods = 8});
            if (!SimulateOrderSignal && MACDLongPeriod > 0) this.signals.Add(new CrossSignal("cross:macd593", Symbol, this, macd, macd.MacdTrigger) { AvgChange = 0.025M, Periods = 4 });
            if (SimulateOrderSignal) this.signals.Add(new FlipFlopSignal("flipflop", Symbol, this, OrderSide.Buy));
            if (this.ProfitQuantity > 0) this.signals.Add(new TakeProfitOrLossSignal("profitOrLoss", Symbol, this, this.ProfitPuan, this.ProfitQuantity, this.LossPuan, this.LossQuantity));
            //if (this.LossQuantity > 0) this.signals.Add(new StopLossSignal("stoploss", Symbol, this, this.LossPuan, this.LossQuantity));

            signals.ForEach(p =>
            {
                p.TimerEnabled = !BackTestMode;
                p.Simulation = BackTestMode;
                p.OnSignal += SignalReceieved;
            });
            
            

            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            orderTimer = new System.Timers.Timer(500);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !BackTestMode)
            {
                AddSymbolMarketData(Symbol);
                //SetTimerInterval(1);
            }
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
        }



        //public void simulateTestPeriod(object barDataCurrentValuesx)
        //{
        //    BarDataCurrentValues barDataCurrentValues = (BarDataCurrentValues)barDataCurrentValuesx;
        //    var t = barDataCurrentValues.LastUpdate.DTime;
        //    var periodsToBack = GetSymbolPeriodSeconds(SymbolPeriod);

        //    var start = t - TimeSpan.FromSeconds(periodsToBack - 1);
        //    Log($"Backtest starting for bar: {t}, start: {start}  seconds back: {periodsToBack}", LogLevel.Debug);
        //    for (var i = 0; i < periodsToBack; i++)
        //    {
        //        var date = start.AddSeconds(i);
        //        foreach (var signal in signals)
        //        {
        //            var t1 = date;
        //            Task.Run(() =>
        //            {
        //                var result = signal.Check(t1);
        //                Log($"used t: {t1}");
        //                SignalReceieved(signal, new SignalEventArgs() { Result = result });
        //            }).Wait();
        //        }
        //        Decide();
        //    }





        //    //var start = new DateTime(2022, 01, 28, 9, 30, 0);
        //    //var end = new DateTime(2022, 01, 28, 18, 0, 0);

        //    //var seconds = (end - start).TotalSeconds;

        //    //for (var i = 0; i < seconds; i++)
        //    //{
        //    //    var t = start.AddSeconds(i);
        //    //    //            var result = this.ManageProfitLoss(price, date);
        //    //    //            if (result.HasValue) break;
        //    //}



        //    //var portfolio = portfolios.GetPortfolio(Symbol);
        //    //if (ProfitQuantity > 0 && !portfolio.IsEmpty)
        //    //{
        //    //    var t = barDataCurrentValues.LastUpdate.DTime;
        //    //    var periodsToBack = GetSymbolPeriodSeconds(SymbolPeriod);

        //    //    var start = t - TimeSpan.FromSeconds(periodsToBack - 1);
        //    //    Log($"Backtest starting to check take profit. Bar: {t}, start: {start}  seconds back: {periodsToBack}", LogLevel.Debug);
        //    //    for (var i = 0; i < periodsToBack; i++)
        //    //    {
        //    //        var date = start.AddSeconds(i);
        //    //        var price = GetMarketPrice(date);
        //    //        if (price != 0)
        //    //        {                        
        //    //            var result = this.ManageProfitLoss(price, date);
        //    //            if (result.HasValue) break;
        //    //        }
        //    //    }
        //    //}

        //    //foreach (var signal in signals)
        //    //{
        //    //    var result = signal.Check();
        //    //    SignalReceieved(signal, new SignalEventArgs() { Result = result });
        //    //}
        //    //Decide();
        //}


        public override void OnInitCompleted()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            Log($"Inited with {assembly.FullName}");
            LoadRealPositions(this.Symbol);
            signals.ForEach(p => p.Start());
            if (!BackTestMode)
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
            if (!this.BackTestMode) return;
      
            foreach (var signal in signals)
            {
                var t = barDataCurrentValues.LastUpdate.DTime;
                var result = signal.Check(t);
                var waitOthers = waitForOperationAndOrders("Backtest");
                //Thread.Sleep(100);


     
                //Log($"test started. {result.Signal.Name} fired {result.finalResult}");
                //SignalReceieved(signal, new SignalEventArgs() { Result = result });

                // Decide();
            }
            //Decide();

            ////backtestWait.WaitOne();
            ////backtestWait.Reset();
            //var t = new Thread(new ParameterizedThreadStart(simulateTestPeriod));
            //t.Start();
            //t.Join();
            ////simulateTestPeriod(barDataCurrentValues);
            ////backtestWait.Set();


        }

        private Boolean waitForOperationAndOrders(string message)
        {
            //Log($"Operations/orders waiting: { message}", LogLevel.Debug);
            
            var result1 = operationWait.WaitOne(1000);
            var result2 = orderWait.WaitOne(1000);
            if (!result1) Log($"operation wait timeout: {message}", LogLevel.Warning);
            if (!result2) Log($"order wait timeout: {message}", LogLevel.Warning);
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
                Log($"Signal {signal.Name} changed from {oldFinalResult} -> {data.Result.finalResult }", LogLevel.Debug);
                if (data.Result.finalResult.HasValue) Decide(signal, data);
            }
        }

        private void HandleProfitLossSignal(TakeProfitOrLossSignal signal, ProfitLossResult result)
        {
            var portfolio = this.UserPortfolioList.GetPortfolio(Symbol);

            if (result.finalResult == OrderSide.Buy && portfolio.IsLong) return;
            if (result.finalResult == OrderSide.Sell && portfolio.IsShort) return;

            var pq = portfolio.Quantity;

            if (pq == this.OrderQuantity)
            {
                Log($"[{result.Signal.Name}:{result.Direction}] received: PL: {result.PL}, MarketPrice: {result.MarketPrice}, Average Cost: {result.PortfolioCost}", LogLevel.Debug);
                sendOrder(Symbol, result.Direction == ProfitOrLoss.Profit ? signal.ProfitQuantity : signal.LossQuantity, result.finalResult.Value, $"[{result.Signal.Name}:{result.Direction}], PL: {result.PL}", result.MarketPrice, result.Direction == ProfitOrLoss.Profit ? ChartIcon.TakeProfit : ChartIcon.StopLoss);
            }
            else Log($"[{result.Signal.Name}:{result.Direction}] received but quantity doesnot match. Portfolio: {pq} oq: {this.OrderQuantity}", LogLevel.Debug);
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
                sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + signalResult.Signal.Name + "]");
            }
        }



        //private void Decide()
        //{
        //    lock(signalResults)
        //    {
        //        var results = signalResults.Where(p => p.Value.finalResult.HasValue).ToList();
        //        List<SignalResultX> validOrders = new List<SignalResultX>();
        //        if (results.Count>0) {
        //            validOrders.Add(results[0].Value);
        //            Log($"Will process signal {results[0].Key} out of {results.Count}", LogLevel.Debug);
        //        } else
        //        {
        //            //Log($"no valid signal result out of {results.Count}");

        //        }

        //        foreach (var validOrder in validOrders)
        //        {
        //            var result = validOrder;
        //            //Log($"Deciding with {signalResults.Count} results from {signals.Count} signals.", LogLevel.Debug);
        //            var waitOthers = waitForOperationAndOrders("Decide");
        //            if (!waitOthers) break;
        //            operationWait.Reset();
        //            try
        //            {
        //                //Log($"Processing signal as {result.finalResult} from {result.Signal.Name}", LogLevel.Debug);
        //                if (result.Signal is TakeProfitOrLossSignal)
        //                {
        //                    var signal = (TakeProfitOrLossSignal)(result.Signal);
        //                    var signalResult = (ProfitLossResult)result;
        //                    var pq = UserPortfolioList.GetPortfolio(Symbol).Quantity;

        //                    if (pq == this.OrderQuantity)
        //                    {
        //                        Log($"{result.Signal.Name} received: PL: {signalResult.PL}, MarketPrice: {signalResult.MarketPrice}, Average Cost: {signalResult.PortfolioCost}", LogLevel.Debug);
        //                        sendOrder(Symbol, signalResult.Direction == ProfitOrLoss.Profit ? signal.ProfitQuantity: signal.LossQuantity, signalResult.finalResult.Value, $"[{result.Signal.Name}], PL: {signalResult.PL}", signalResult.MarketPrice, signalResult.Direction == ProfitOrLoss.Profit ? ChartIcon.TakeProfit: ChartIcon.StopLoss);
        //                    }
        //                    else Log($"TakeProfitLoss received but quantity doesbot match. Portfolio: {pq} oq: {this.OrderQuantity}");
        //                }
        //                //else if (result.Signal is StopLossSignal)
        //                //{
        //                //    var lossSignal = (StopLossSignal)(result.Signal);
        //                //    var profitResult = (ProfitLossResult)result;
        //                //    var pq = UserPortfolioList.GetPortfolio(Symbol).Quantity;

        //                //    if (pq == this.OrderQuantity)
        //                //    {
        //                //        Log($"{result.Signal.Name} received: PL: {profitResult.PL}, MarketPrice: {profitResult.MarketPrice}, Average Cost: {profitResult.PortfolioCost}", LogLevel.Debug);
        //                //        sendOrder(Symbol, lossSignal.Quantity, profitResult.finalResult.Value, $"[{result.Signal.Name}], PL: {profitResult.PL}", profitResult.MarketPrice, ChartIcon.StopLoss);
        //                //    }
        //                //    else Log($"Stoploss received but quantity doesbot match. Portfolio: {pq} oq: {this.OrderQuantity}");
        //                //}
        //                else CreateOrders(result);

        //            }
        //            finally
        //            {
        //                operationWait.Set();
        //            }

        //        }
        //    }

        //}


        protected void sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0, ChartIcon icon = ChartIcon.None)
        {
            //Log($"Order received: {symbol} {quantity} {side} {comment}", LogLevel.Debug);
            orderWait.Reset();
            var price = lprice > 0 ? lprice : GetMarketPrice(this.Symbol);
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
                this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment);
                Log($"Order created, waiting to complete: {this.positionRequest.ToString()}");
                if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
            }            
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
            Log($"Completed order: {this.positionRequest.ToString()}");
            Log($"Portfolio: {portfolio.ToString()}");
            this.positionRequest = null;
            orderWait.Set();
        }

        public override void OnOrderUpdate(IOrder order)
        {
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                Log($"OrderUpdate: pos: {this.positionRequest} status: {order.OrdStatus.Obj} orderid: {order.CliOrdID} fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}", LogLevel.Debug);

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
            Log($"Portfolio ended: {UserPortfolioList.Print()}");
        }
    }

}

