﻿// algo
using Skender.Stock.Indicators;
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
        decimal GetVolume(string symbol, BarPeriod period, DateTime? t = null);
    }

    public interface IExchange : IMarketDataProvider
    {
        string CreateMarketOrder(string symbol, decimal quantity, BuySell side, string icon, bool night);
        string CreateLimitOrder(string symbol, decimal quantity, BuySell side, decimal limitPrice, string icon, bool night);
        void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null);
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

        public VolatileRatio VolatileRatio { get; set; } = VolatileRatio.Average;

        public IExchange Exchange { get; set; }

        [AlgoParam()]
        public LogLevel LoggingLevel { get; set; } = LogLevel.Verbose;

        [AlgoParam()]
        public bool Simulation { get; set; } = false;

        public string LogDir { get; set; } = @"c:\kalitte\log";
        public MarketDataFileLogger PriceLogger;
        public string InstanceName { get; set; }

        [AlgoParam("F_XU0300222")]
        public string Symbol { get; set; } = "F_XU0300222";


        [AlgoParam(BarPeriod.Min10)]
        public BarPeriod SymbolPeriod { get; set; } = BarPeriod.Min10;


        [AlgoParam()]
        public bool UseVirtualOrders { get; set; }

        [AlgoParam()]
        public bool AutoCompleteOrders { get; set; }


        protected DateTime? TimeSet = null;

        public PortfolioList UserPortfolioList = new PortfolioList();
        public decimal simulationPriceDif = 0;

        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        public FinanceBars PeriodBars = null;
        int orderCounter = 0;

        public Dictionary<string, decimal> ordersBySignals = new Dictionary<string, decimal>();

        public Dictionary<string, PerformanceCounter> perfCounters = new Dictionary<string, PerformanceCounter>();

        int virtualOrderCounter = 0;
        public ExchangeOrder positionRequest = null;
        public int simulationCount = 0;
        public StartableState SignalsState { get; private set; } = StartableState.Stopped;

        public ManualResetEvent orderWait = new ManualResetEvent(true);
        public ManualResetEvent operationWait = new ManualResetEvent(true);

        public abstract void Decide(Signal signal, SignalEventArgs data);

        public bool WaitSignalOperations(int timeOut = 1000)
        {
            return ManualResetEvent.WaitAll(Signals.Select(p => p.InOperationLock).ToArray(), timeOut);
        }


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
            if (this.positionRequest.SignalResult != null) CountOrder(this.positionRequest.SignalResult.Signal.Name, filledQuantity);
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
                if (ordersBySignals.TryGetValue(signal, out decimal existing))
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
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 18, 20, 0);
            var t3 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 0);
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

        public virtual IQuote PushNewBar(string symbol, BarPeriod period, DateTime date, IQuote bar = null)
        {
            if (this.Symbol == symbol && this.SymbolPeriod == period)
            {
                if (bar == null)
                {
                    var lastBar = this.GetPeriodBars(symbol, period, date).Last;
                    if (lastBar.Date != date)
                    {
                        Log($"{symbol} {period} {date} bar couldn't be retreived", LogLevel.Error);
                    }
                    else bar = lastBar;
                }
                if (bar != null)
                {
                    PeriodBars.Push(bar);
                    Log($"Pushed new bar, last bar is now: {PeriodBars.Last}", LogLevel.Debug, date);
                    return bar;
                }
            }
            return null;
        }



        public virtual void InitializeBars(string symbol, BarPeriod period, DateTime t)
        {
            this.PeriodBars = GetPeriodBars(symbol, period, t);
            Log($"Initialized total {PeriodBars.Count} using time {t}. Last bar is: {PeriodBars.Last}", LogLevel.Debug, t);
        }

        public virtual FinanceBars GetPeriodBars(string symbol, BarPeriod period, DateTime t)
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
                    var mdp = new MarketDataFileLogger(symbol, LogDir, period.ToString());
                    mdp.FileName = "all.txt";
                    mdp.SaveDaily = true;
                    periodBars = mdp.GetContentAsQuote(t);
                    periodBars.Period = period;
                }
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
            int? oldHashCode = null;
            lock (SignalResults)
            {
                if (SignalResults.TryGetValue(signal.Name, out existing))
                {
                    oldFinalResult = existing.finalResult;
                    oldHashCode = existing.GetHashCode();
                }
                SignalResults[signal.Name] = data.Result;
            }
            if (oldHashCode != data.Result.GetHashCode())
            //if (oldFinalResult != data.Result.finalResult)
            {
                Log($"Signal {signal.Name} changed from {existing} -> {data.Result }", LogLevel.Verbose, data.Result.SignalTime);
                if (data.Result.finalResult.HasValue) Decide(signal, data);
            }
        }

        public StringBuilder PropSummary()
        {
            var properties = this.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));

            //for (var i = 0; i < properties.Count(); i++)
            //{
            //    var line = $"{properties[i].Name}\t{properties[i].GetValue(this)}";
            //    Log($"{line}");                
            //}
            StringBuilder sb = new StringBuilder("\n-- USED PARAMETERS --\n");
            foreach (var item in properties)
            {
                sb.Append($"{item.Name} \t {item.GetValue(this)}\n");
            }
            sb.Append("----------\n");
            return sb;
        }




        public virtual void Init()
        {



        }

        public virtual void InitMySignals(DateTime t)
        {

        }




        public virtual void InitCompleted()
        {
            //CreatePerformanceCounters();
            if (!Simulation)
            {
                Log($"Setting seans timer ...");
                seansTimer = new System.Timers.Timer(1000);
                seansTimer.Enabled = true;
                seansTimer.Elapsed += SeansTimer_Elapsed;
            }
            else StartSignals();
        }

        public void SetPerformanceCounterValue(string name, decimal value)
        {
            var counter = perfCounters[name];
            counter.IncrementBy(1);
        }

        private void CreatePerformanceCounters()
        {
            if (!PerformanceCounterCategory.Exists(InstanceName))
            {

                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection();

                foreach (var signal in this.Signals)
                {
                    CounterCreationData ccd = new CounterCreationData();
                    ccd.CounterType = PerformanceCounterType.AverageBase;
                    ccd.CounterName = signal.Name;
                    counterDataCollection.Add(ccd);
                }

                PerformanceCounterCategory.Create(InstanceName,
                    "",
                    PerformanceCounterCategoryType.SingleInstance, counterDataCollection);
            }

            foreach (var signal in this.Signals)
            {
                var counter = new PerformanceCounter(InstanceName, signal.Name);
                perfCounters[signal.Name] = counter;
            }




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
            Log(PropSummary().ToString(), LogLevel.FinalResult);
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
            var price = lprice > 0 ? lprice : this.GetMarketPrice(symbol, t);
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
            order.SignalResult = signalResult;
            order.Sent = t ?? DateTime.Now;

            Log($"New order submitted. Market price was: {price}: {this.positionRequest.ToString()}", LogLevel.Order, t);
            if (order.SignalResult != null)
                Log($"Signal [{order.SignalResult.Signal.Name}] result: {order.SignalResult}", LogLevel.Order, t);
            Log($"Used bar: {this.PeriodBars.Last}", LogLevel.Order, t);

            if (this.UseVirtualOrders || this.AutoCompleteOrders)
            {
                if (this.Simulation)
                {
                    var algoTime = AlgoTime;
                    //this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(0.5 + new RandomGenerator().NextDouble() * 2) };
                    this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(1.2) };
                    Log($"Simulating real environment for {delayedOrder.order.Id} time is: {delayedOrder.created}, schedule to: {delayedOrder.scheduled2}", LogLevel.Debug);
                }
                else FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
            }
        }


        public int GetSymbolPeriodSeconds(string period)
        {
            int result;
            Helper.SymbolSeconds(period, out result);
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
                    if (Exchange != null) Exchange.Log(content, level, t);
                }
            }
        }

        public DateTime NextBar(DateTime current, BarPeriod period)
        {
            Helper.SymbolSeconds(period.ToString(), out int periodSeconds);
            var minutes = periodSeconds / 60;
            var expected = current.AddSeconds(periodSeconds);
            if (expected.Hour >= 23) expected = current.Date.AddHours(24 + 9.5);
            else if (expected.Hour == 18 && expected.Minute >= 10) expected = current.Date.AddHours(19);
            return expected;
        }

        public decimal[] GetMarketData(string symbol, BarPeriod period, DateTime? t = null)
        {
            var values = PriceLogger.GetMarketDataList(t.Value);
            if (values.Length == 0)
            {
                int toBack = 0, toForward = 0;
                while (toBack-- > -5)
                {
                    toForward++;
                    values = PriceLogger.GetMarketDataList(t.Value.AddSeconds(toBack));
                    if (values.Length > 0) return values;
                    values = PriceLogger.GetMarketDataList(t.Value.AddSeconds(toForward));
                    if (values.Length > 0) return values;

                }
            }
            return values;
        }

        public virtual decimal GetVolume(string symbol, BarPeriod period, DateTime? t = null)
        {
            if (Simulation)
            {
                var list = GetMarketData(symbol, SymbolPeriod, t);
                var price = list.Length > 0 ? list[1] : 0;
                return price;
            }
            else return Exchange.GetVolume(symbol, period, t);
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            if (Simulation)
            {
                var list = GetMarketData(symbol, SymbolPeriod, t);
                var price = list.Length > 0 ? list[0]: 0;
                return price;
            }
            else return Exchange.GetMarketPrice(symbol, t);
        }


    }
}
