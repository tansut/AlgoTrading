// algo
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading.Algos
{

    //public interface IAlgo
    //{
    //    bool Simulation { get; set; }
    //    public LogLevel LogLevel { get; set; }
    //    string InstanceName { get; set; }
    //    DateTime? TimeSet { get; set; }
    //    void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null);
    //    decimal GetMarketPrice(string symbol, DateTime? t = null);
    //}

    public interface IMarketDataProvider
    {
        decimal GetMarketPrice(string symbol, DateTime? t = null);
    }

    public interface IExchange: IMarketDataProvider
    {
        string CreateMarketOrder(string symbol, decimal quantity, BuySell side, string icon, bool night);
        string CreateLimitOrder(string symbol, decimal quantity, BuySell side, decimal limitPrice, string icon, bool night);

    }

    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }

    public abstract class AlgoBase
    {
        public static AlgoBase Current;

        public IExchange Exchange { get; set; }

        [AlgoParam()]
        public LogLevel LoggingLevel { get; set; } = LogLevel.Info;

        [AlgoParam()]
        public bool Simulation { get; set; } = false;
        public string LogDir { get; set; } = @"c:\kalitte\log";
        public MarketDataFileLogger PriceLogger;
        public string InstanceName { get; set; }

        [AlgoParam()]
        public string Symbol { get; set; }        
        
        [AlgoParam()]
        public BarPeriod SymbolPeriod { get; set; }

        [AlgoParam()]
        public bool UseVirtualOrders { get; set; }

        [AlgoParam()]
        public bool AutoCompleteOrders { get; set; }
        

        protected DateTime? TimeSet = null;

        public PortfolioList UserPortfolioList = new PortfolioList();
        public decimal simulationPriceDif = 0;
        private static Dictionary<string, int> symbolPeriodCache = new Dictionary<string, int>();

        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        public FinanceBars PeriodBars = null;        
        int orderCounter = 0;

        public Dictionary<string, decimal> ordersBySignals = new Dictionary<string, decimal>();

        int virtualOrderCounter = 0;
        public ExchangeOrder positionRequest = null;
        public int simulationCount = 0;
        public StartableState SignalsState { get; private set; } = StartableState.Stopped;

        public ManualResetEvent orderWait = new ManualResetEvent(true);
        public ManualResetEvent operationWait = new ManualResetEvent(true);

        public abstract void Decide(Signal signal, SignalEventArgs data);


        public void CheckDelayedOrders(DateTime t)
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

        public void CancelCurrentOrder(string reason)
        {
            Log($"Order rejected/cancelled [{reason}]", LogLevel.Warning, this.positionRequest.Created);
            this.positionRequest = null;
            orderWait.Set();
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
                foreach (var signal in Signals)
                {
                    signal.Start();
                    Log($"Started signal {signal}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting signals: {ex.Message}, {ex.Message}", LogLevel.Error);
            }

            SignalsState = StartableState.Started; ;
        }

        public void StopSignals()
        {
            SignalsState = StartableState.StopInProgress;
            try
            {
                foreach (var signal in Signals)
                {

                    signal.Stop();
                    Log($"Stopped signal {signal}", LogLevel.Info);

                }
            }
            catch (Exception ex)
            {
                Log($"Error stopping signals: {ex.Message}, {ex.StackTrace}", LogLevel.Error);
            }

            SignalsState = StartableState.Stopped;


        }

        private void SeansTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 1);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 18, 15, 0);
            var t3 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 1);
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



        public void LoadBars(DateTime t)
        {
            this.PeriodBars = GetPeriodBars(t);
        }

        public FinanceBars GetPeriodBars(DateTime t)
        {
            
                var periodBars = new FinanceBars();
                try
                {
                    //var bd = GetBarData(Symbol, SymbolPeriod);
                    //if (bd != null && bd.BarDataIndexer != null)
                    //{
                    //    for (var i = 0; i < bd.BarDataIndexer.LastBarIndex; i++)
                    //    {
                    //        if (bd.BarDataIndexer[i] > t) break;
                    //        var quote = new MyQuote() { Date = bd.BarDataIndexer[i], Open = bd.Open[i], High = bd.High[i], Low = bd.Low[i], Close = bd.Close[i], Volume = bd.Volume[i] };
                    //        periodBars.Push(quote);
                    //    }
                    //}
                    //else
                    {
                        var mdp = new MarketDataFileLogger(Symbol, LogDir, SymbolPeriod.ToString());
                        mdp.FileName = "all.txt";
                        mdp.SaveDaily = true;
                        periodBars = mdp.GetContentAsQuote(t);
                    }
                    Log($"Initialized total {periodBars.Count} using time {t}. Last bar is: {periodBars.Last}", LogLevel.Debug, t);
                }
                catch (Exception ex)
                {
                    Log($"Error initializing bars {ex.Message}", LogLevel.Error, t);
                }

            return periodBars;
            
        }


        public AlgoBase()
        {
            RandomGenerator random = new RandomGenerator();
            if (!Directory.Exists(Path.GetDirectoryName(LogFile))) Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
            this.InstanceName = this.GetType().Name + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + (random.Next(1000000, 9999999));
            Current = this;
        }

        public Boolean waitForOperationAndOrders(string message)
        {
            var wait = Simulation ? 0 : 100;
            var result1 = operationWait.WaitOne(wait);
            var result2 = orderWait.WaitOne(wait);
            if (!result1 && !Simulation) Log($"Waiting for last operation to complete: {message}", LogLevel.Warning);
            if (!result2 && !Simulation) Log($"Waiting for last order to complete: {message}", LogLevel.Warning);
            return result1 && result2;
        }

        public void SignalReceieved(Signal signal, SignalEventArgs data)
        {
            SignalResultX existing;
            BuySell? oldFinalResult = null;
            lock (SignalResults)
            {
                if (SignalResults.TryGetValue(signal.Name, out existing))
                    oldFinalResult = existing.finalResult;
                SignalResults[signal.Name] = data.Result;
            }
            if (oldFinalResult != data.Result.finalResult)
            {
                Log($"Signal {signal.Name} changed from {oldFinalResult} -> {data.Result.finalResult }", LogLevel.Debug, data.Result.SignalTime);
                if (data.Result.finalResult.HasValue) Decide(signal, data);
            }
        }


        public virtual void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
        }

        public virtual void InitMySignals(DateTime t)
        {

        }

        public virtual void InitCompleted()
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

        public virtual void Stop()
        {
            if (!Simulation)
            {
                seansTimer.Stop();
                seansTimer.Dispose();
            }
            StopSignals();

            Log($"Completed {this}", LogLevel.FinalResult);
            Signals.ForEach(p => Log($"{p}", LogLevel.FinalResult));
            Log($"----------------------", LogLevel.FinalResult);
            Log($"Market price difference total: {this.simulationPriceDif}", LogLevel.FinalResult);
            foreach (var item in ordersBySignals)
            {
                Log($"{item.Key}:{item.Value}", LogLevel.FinalResult);
            }
            Log($"Total orders filled:: {this.orderCounter}", LogLevel.FinalResult);
            Log($"{printPortfolio()}", LogLevel.FinalResult);
            Log($"----------------------", LogLevel.FinalResult);


        }

        public void sendOrder(string symbol, decimal quantity, BuySell side, string comment = "", decimal lprice = 0, OrderIcon icon = OrderIcon.None, DateTime? t = null, SignalResultX signalResult = null)
        {
            orderWait.Reset();
            var price = lprice > 0 ? lprice : this.GetMarketPrice(this.Symbol, t);
            if (price == 0)
            {
                Log($"Unable to get a marketprice at {t}, using close {PeriodBars.Last.Close} from {PeriodBars.Last}", LogLevel.Warning, t);
                price = PeriodBars.Last.Close;
            }
            string orderid;
            decimal limitPrice = Math.Round((price + price * 0.02M * (side == BuySell.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;

            if (UseVirtualOrders)
            {
                orderid = virtualOrderCounter++.ToString();
            }
            else
            {
                orderid = Simulation ? Exchange.CreateMarketOrder(symbol, quantity, side, icon.ToString(), (t ?? DateTime.Now).Hour >= 19) :
                    Exchange.CreateLimitOrder(symbol, quantity, side, limitPrice, icon.ToString(), (t ?? DateTime.Now).Hour >= 19);
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


        public int GetSymbolPeriodSeconds(string period)
        {
            int result;
            symbolPeriodCache.TryGetValue(period, out result);
            if (result == 0) throw new ArgumentException("Not supported period");
            return result;
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

        static AlgoBase()
        {
            symbolPeriodCache.Add(BarPeriod.Min.ToString(), 60);
            symbolPeriodCache.Add(BarPeriod.Min5.ToString(), 5 * 60);
            symbolPeriodCache.Add(BarPeriod.Min10.ToString(), 10 * 60);
            symbolPeriodCache.Add(BarPeriod.Min15.ToString(), 15 * 60);
            symbolPeriodCache.Add(BarPeriod.Min20.ToString(), 20 * 60);
            symbolPeriodCache.Add(BarPeriod.Min30.ToString(), 30 * 60);
            symbolPeriodCache.Add(BarPeriod.Min60.ToString(), 60 * 60);
            symbolPeriodCache.Add(BarPeriod.Min120.ToString(), 120 * 60);
            symbolPeriodCache.Add(BarPeriod.Min180.ToString(), 180 * 60);
            symbolPeriodCache.Add(BarPeriod.Min240.ToString(), 180 * 60);
        }

        public List<Signal> Signals = new List<Signal>();
        public ConcurrentDictionary<string, SignalResultX> SignalResults = new ConcurrentDictionary<string, SignalResultX>();


        public DateTime AlgoTime
        {
            get
            {
                return TimeSet ?? DateTime.Now;
            }
             set
            {
                TimeSet = value;
            }
        }

        public string LogFile
        {
            get
            {
                return Path.Combine(LogDir, $"algologs{(Simulation ? 'B' : 'L')}", $" {InstanceName}.txt");
            }
        }

        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            if ((int)level >= (int)this.LoggingLevel)
            {
                var time = t ?? AlgoTime;
                string opTime = time.ToString("yyyy.MM.dd HH:mm:sss");
                var content = $"[{level}:{opTime}]: {text}" + Environment.NewLine;
                lock (this)
                {
                    Console.WriteLine(content);
                    File.AppendAllText(LogFile, content + Environment.NewLine);
                }
            }
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            if (Simulation)
            {
                var price = PriceLogger.GetMarketData(t.Value) ?? 0;
                if (price == 0)
                {
                    int toBack = 0, toForward = 0;
                    while (toBack-- > -5)
                    {
                        toForward++;
                        price = PriceLogger.GetMarketData(t.Value.AddSeconds(toBack)) ?? 0;
                        if (price > 0) return price;
                        price = PriceLogger.GetMarketData(t.Value.AddSeconds(toForward)) ?? 0;
                        if (price > 0) return price;

                    }
                }
                return price;
            }
            else return Exchange.GetMarketPrice(symbol, t);

        }


    }
}
