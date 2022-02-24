// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
        FinanceBars GetPeriodBars(string symbol, BarPeriod period, DateTime t);
    }

    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }

    public class SymbolData
    {
        public string Symbol { get; private set; }
        public FinanceBars Periods { get; private set; }


        //public SymbolData(string symbol, BarPeriod period)
        //{
        //    Symbol = symbol;
        //    Periods = new FinanceBars();
        //    Periods.Period = period;
        //}

        public SymbolData(string symbol, FinanceBars periods)
        {
            Symbol = symbol;
            Periods = periods;
            //Periods = new FinanceBars();
            //Periods.Period = period;
        }
    }

    public abstract class AlgoBase
    {
        Dictionary<string, MarketDataFileLogger> dataProviders = new Dictionary<string, MarketDataFileLogger>();

        public static AlgoBase Current;

        public List<SymbolData> Symbols { get; set; } = new List<SymbolData>();

        public VolatileRatio VolatileRatio { get; set; } = VolatileRatio.Average;

        public IExchange Exchange { get; set; }

        public DateTime? TestStart { get; set; }
        public DateTime? TestFinish { get; set; }

        [AlgoParam(LogLevel.Verbose)]
        public LogLevel LoggingLevel { get; set; }

        [AlgoParam(false)]
        public bool Simulation { get; set; }

        [AlgoParam(true)]
        public bool LogConsole { get; set; }

        [AlgoParam(@"c:\kalitte\log")]
        public string LogDir { get; set; }

        public MarketDataFileLogger PriceLogger;
        public string InstanceName { get; set; }

        [AlgoParam("F_XU0300222")]
        public string Symbol { get; set; }


        [AlgoParam(BarPeriod.Min10)]
        public BarPeriod SymbolPeriod { get; set; }


        [AlgoParam(false)]
        public bool UseVirtualOrders { get; set; }

        [AlgoParam(false)]
        public bool AutoCompleteOrders { get; set; }


        protected DateTime? TimeSet = null;

        public PortfolioList UserPortfolioList = new PortfolioList();
        public decimal simulationPriceDif = 0;

        public void AddSymbol(SymbolData data)
        {

        }

        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        //public FinanceBars PeriodBars = null;
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

        public virtual IQuote PushNewBar(string symbol, BarPeriod period, IQuote bar)
        {
            var data = GetSymbolData(symbol, period);
            data.Periods.Push(bar);
            Log($"Pushed new bar, last bar is now: {data.Periods.Last}", LogLevel.Debug, data.Periods.Last.Date);
            return bar;
        }

        public SymbolData GetSymbolData(string symbol, BarPeriod period)
        {
            return Symbols.Where(p => p.Symbol == symbol && p.Periods.Period == period).FirstOrDefault();
        }



        public virtual void InitializeBars(string symbol, BarPeriod period, DateTime? t)
        {                        
            var periodBars = GetPeriodBars(symbol, period, t);
            //var existing = this.Symbols.Select(p => p.Symbol == symbol).FirstOrDefault();
            //if (existing)
            this.Symbols.Add(new SymbolData(symbol, periodBars));
            Log($"Initialized total {periodBars.Count} for {symbol} using time {t}. Last bar is: {periodBars.Last}", LogLevel.Debug, t);
        }

        public virtual FinanceBars GetPeriodBars(string symbol, BarPeriod period, DateTime? t = null)
        {
            FinanceBars periodBars = null;
            try
            {
                //periodBars = Exchange != null ? Exchange.GetPeriodBars(symbol, period, t): null;                
                if (periodBars == null)
                {
                    dataProviders.TryGetValue(symbol + period.ToString(), out MarketDataFileLogger mdp);
                    if (mdp == null)
                    {
                        mdp = new MarketDataFileLogger(symbol, LogDir, period.ToString());
                        mdp.FileName = "all.txt";
                        mdp.SaveDaily = true;
                        dataProviders[symbol + period.ToString()] = mdp;
                    }                    
                    periodBars = mdp.GetContentAsQuote(t ?? DateTime.Now);                    
                }
                periodBars.Period = period;
            }
            catch (Exception ex)
            {
                Log($"Error initializing bars {ex.Message}", LogLevel.Error, t);
            }

            return periodBars;

        }

        public AlgoBase(): this(null)
        {

        }


        public AlgoBase(Dictionary<string, object> initValues)
        {
            this.ApplyProperties(initValues);
            RandomGenerator random = new RandomGenerator();
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

        public static Dictionary<string, object> GetProperties(Type type)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var properties = type.GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
            foreach (var prop in properties)
            {
                var attr = (AlgoParam)prop.GetCustomAttributes(true).Where(p => p is AlgoParam).First();
                result.Add(prop.Name, attr.Value);
            }
            return result;

        }

        public void ApplyProperties(Dictionary<string, object> init = null)
        {
            if (init == null) init = GetProperties(this.GetType());
            var properties = this.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
            foreach (var item in properties)
            {
                object val;
                if (init.TryGetValue(item.Name, out val))
                {
                    object propValue = val;
                    if (val != null && val.GetType() != item.PropertyType)
                    {
                        if (item.PropertyType == typeof(decimal))
                        {
                            propValue = Convert.ToDecimal(val);
                        }
                        else if (item.PropertyType == typeof(int))
                        {
                            propValue = Convert.ToInt32(val);
                        } else if(item.PropertyType == typeof(LogLevel))
                        {
                            propValue = (LogLevel)Convert.ToInt32(val);
                        }
                        else if (item.PropertyType == typeof(BarPeriod))
                        {
                            propValue = (BarPeriod)Convert.ToInt32(val);
                        }
                        //if (typeof(propValue) != propValue)
                        //var tc = new TypeConverter();
                        //propValue = tc.ConvertTo(val, item.PropertyType);
                    }
                    this.GetType().GetProperty(item.Name).SetValue(this, propValue);
                }
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
            if (!Directory.Exists(Path.GetDirectoryName(LogFile))) Directory.CreateDirectory(Path.GetDirectoryName(LogFile));


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
            if (TestStart.HasValue)
            {
                Log($"For dates {TestStart} - {TestFinish}", LogLevel.FinalResult);
            }
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

        public virtual void sendOrder(string symbol, decimal quantity, BuySell side, string comment = "", decimal lprice = 0, OrderIcon icon = OrderIcon.None, DateTime? t = null, SignalResultX signalResult = null)
        {
            orderWait.Reset();
            var symbolData = GetSymbolData(symbol, this.SymbolPeriod);
            var price = lprice > 0 ? lprice : this.GetMarketPrice(symbol, t);
            if (price == 0)
            {
                Log($"Unable to get a marketprice at {t}, using close {symbolData.Periods.Last.Close} from {symbolData.Periods.Last}", LogLevel.Warning, t);
                price = symbolData.Periods.Last.Close;
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
            Log($"Used bar: {symbolData.Periods.Last}", LogLevel.Order, t);

            if (this.UseVirtualOrders || this.AutoCompleteOrders)
            {
                if (this.Simulation)
                {
                    var algoTime = AlgoTime;
                    //this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(0.5 + new RandomGenerator().NextDouble() * 2) };
                    this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(2.5) };
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
                var inner = this.Simulation ? "simulation" : "live";
                return Path.Combine(LogDir, inner, $"{InstanceName}.txt");
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
                    if (LogConsole) Console.WriteLine(content);
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
