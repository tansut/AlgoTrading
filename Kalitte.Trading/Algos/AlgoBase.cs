// algo
using Newtonsoft.Json;
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



    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }

    [Serializable]
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

    public abstract class AlgoBase: ILogProvider
    {

        Mutex simulationFileMutext = new Mutex(false, "simulationFileMutext");

        Dictionary<string, MarketDataFileLogger> dataProviders = new Dictionary<string, MarketDataFileLogger>();

        public static AlgoBase Current;

        public List<SymbolData> Symbols { get; set; } = new List<SymbolData>();

        public VolatileRatio VolatileRatio { get; set; } = VolatileRatio.Average;

        public IExchange Exchange { get; set; }

        public DateTime? TestStart { get; set; }
        public DateTime? TestFinish { get; set; }
        public string SimulationFile { get; set; } = "";
        public string[] SimulationFileFields { get; set; } = new string[] { };

        private DateTime? time { get; set; } = null;

        private object simulationLock = new object();

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

        [AlgoParam("F_XU0300422")]
        public string Symbol { get; set; } = "F_XU0300422";

        [AlgoParam(0)]
        public decimal ExpectedNetPl { get; set; }

        [AlgoParam(BarPeriod.Min10)]
        public BarPeriod SymbolPeriod { get; set; }


        [AlgoParam(false)]
        public bool UseVirtualOrders { get; set; }

        [AlgoParam(false)]
        public bool ClosePositionsDaily { get; set; }



        public StringBuilder LogContent { get; set; } = new StringBuilder(1000);



        public PortfolioList UserPortfolioList = new PortfolioList();
        public decimal simulationPriceDif = 0;

        public void AddSymbol(SymbolData data)
        {

        }

        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        internal DateTime SetTime(DateTime t)
        {
            return (DateTime)(time = t);
        }

        public DateTime Now
        {
            get
            {
                return time ?? DateTime.Now;
            }
        }

        public PerformanceMonitor Monitor = new PerformanceMonitor();


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
                var dif = Now - delayedOrder.scheduled2;
                if (dif.Seconds >= 0)
                {
                    Log($"Simulation completed at {t}  for {delayedOrder.order.Id}", LogLevel.Debug);
                    FillCurrentOrder(delayedOrder.order.UnitPrice, delayedOrder.order.Quantity);
                    this.delayedOrder = null;
                }
            }
        }

        public virtual void ConfigureMonitor()
        {
            this.Monitor.DefaultChange = 25.0M;
            this.Monitor.MonitorEvent += Monitor_MonitorEvent;
        }

        protected virtual void Monitor_MonitorEvent(object sender, MonitorEventArgs e)
        {
            if (e.EventType == MonitorEventType.Updated)
                Log($"{e}", LogLevel.Debug);
        }

        public virtual void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            this.positionRequest.FilledUnitPrice = filledUnitPrice;
            this.positionRequest.FilledQuantity = filledQuantity;
            var portfolio = this.UserPortfolioList.Add(this.positionRequest);
            var port = UserPortfolioList.Where(p => p.Key == positionRequest.Symbol).First().Value;
            Log($"Filled[{port.SideStr}/{port.Quantity}/{port.AvgCost.ToCurrency()} NetPL:{port.NetPL.ToCurrency()}]: {this.positionRequest.ToString()}", LogLevel.Order);
            CountOrder(this.positionRequest.SignalResult.Signal.Name, filledQuantity);
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


        public DateTime GetExpectedBarPeriod(DateTime t, BarPeriod period)
        {
            Helper.SymbolSeconds(period.ToString(), out int seconds);
            var shouldBe = Helper.RoundDown(t, TimeSpan.FromSeconds(seconds)).AddSeconds(-seconds);
            var mMax = 30 + TimeSpan.FromSeconds(seconds).Minutes;
            var nMax = TimeSpan.FromSeconds(seconds).Minutes;
            if (t.Hour == 9 && t.Minute < mMax)
            {
                var last = t.Date.AddDays(-1).AddHours(23).AddSeconds(-seconds);
                while (last.DayOfWeek == DayOfWeek.Sunday || last.DayOfWeek == DayOfWeek.Saturday)
                {
                    last = last.AddDays(-1);
                }
                shouldBe = last;
            }
            else if (t.Hour == 19 && t.Minute < nMax)
            {
                shouldBe = t.Date.AddHours(18).AddMinutes(10);
            }
            return shouldBe;
        }

        //internal bool EnsureRightBar(DateTime t, BarPeriod period)
        //{
        //    var sholdBe
        //    if (t.Hour == 9 && t.Minute < 10)
        //    {
        //        var last = new DateTime(). AddDays(-1).AddHours()
        //        shouldBe = 
        //    }
        //    throw new NotImplementedException();
        //}

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

        public bool IsMorningStart(DateTime? t = null)
        {
            var time = t ?? Now;
            return time.Hour == 9 && time.Minute == 30;
        }

        public void SetBarCurrentValues()
        {
            var time = Now;
            var secDict = new Dictionary<int, BarPeriod>();
            var secondsToStop = Symbols.Select(p => secDict[GetSymbolPeriodSeconds(p.Periods.Period.ToString())] = p.Periods.Period).ToList();
            //var round = Helper.RoundDown(p, TimeSpan.FromSeconds(sec.Key));

            foreach (var sec in secDict)
            {
                var round = Helper.RoundUp(time, TimeSpan.FromSeconds(sec.Key));
                var roundDown = Helper.RoundDown(time, TimeSpan.FromSeconds(sec.Key));
            }

                
            foreach (var sd in Symbols)
            {
                var p = sd.Periods;
                var close = GetMarketPrice(p.Symbol, Now);
                var vol = GetVolume(p.Symbol, p.Period, Now);
                p.SetCurrent(Now, close, vol);
            }
        }

        private void SeansTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            seansTimer.Enabled = false;
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 18, 15, 0);
            var t3 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 0);
            var t4 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);
            try
            {
                if ((t >= t1 && t <= t2) || (t >= t3 && t <= t4))
                {
                    //SetBarCurrentValues();
                    if (SignalsState == StartableState.Stopped)
                    {
                        Log($"Time seems OK, starting signals ...");
                        if (IsMorningStart(t)) Signals.ForEach(p => p.Reset());
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

        public virtual void PushNewBar(string symbol, BarPeriod period, IQuote bar)
        {
            var data = GetSymbolData(symbol, period);
            if (data == null)
            {
                Log($"Received new bar for period {period}, but algo doesnot use it", LogLevel.Warning);
            }
            else
            {
                data.Periods.Push(bar);
                data.Periods.ClearCurrent();
                Log($"Pushed new bar for period {period}, last bar is now: {data.Periods.Last}", LogLevel.Debug, data.Periods.Last.Date);
            }
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
            Log($"Initialized total {periodBars.Count} for {symbol}/{period} using time {t}. Last bar is: {periodBars.Last}", LogLevel.Debug, t);
        }

        public virtual FinanceBars GetPeriodBars(string symbol, BarPeriod period, DateTime? t = null)
        {
            FinanceBars periodBars = null;
            try
            {
                periodBars = Exchange != null ? Exchange.GetPeriodBars(symbol, period, t ?? DateTime.Now) : null;
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
                    periodBars = mdp.GetContentAsQuote(symbol, period, t ?? DateTime.Now);
                }
                var total = periodBars.Count;
                periodBars.RecommendedSkip = Math.Min(total, total - 48);

            }
            catch (Exception ex)
            {
                Log($"Error initializing bars {ex.Message}", LogLevel.Error, t);
            }

            return periodBars;

        }

        public AlgoBase() : this(null)
        {

        }

        //public AlgoBase(string configFile)
        //{


        //}


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
            SignalResult existing;
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
            {
                Log($"Signal {signal.Name} changed from {existing} -> {data.Result }", LogLevel.Verbose, data.Result.SignalTime);
                if (data.Result.finalResult.HasValue) Decide(signal, data);
            }
        }

        public Dictionary<string, object> GetConfigValues()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var properties = this.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
            foreach (var prop in properties)
            {
                var attr = (AlgoParam)prop.GetCustomAttributes(true).Where(p => p is AlgoParam).First();
                result.Add(prop.Name, prop.GetValue(this));
            }
            return result;
        }

        public static Dictionary<string, object> GetConfigValues(Type type)
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
            if (init == null) init = GetConfigValues(this.GetType());
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
                        }
                        else if (item.PropertyType == typeof(LogLevel))
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
                else
                {
                    var paramVal = item.GetCustomAttributes(typeof(AlgoParam), true).Cast<AlgoParam>().FirstOrDefault();
                    if (paramVal != null) item.SetValue(this, paramVal.Value);
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
            ConfigureMonitor();
            this.Monitor.Start();
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
            this.Monitor.Stop();

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

            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;

            var saveLog = true;
            if (Simulation && ExpectedNetPl != 0 && netPL < ExpectedNetPl)
            {
                saveLog = false;
            }
            if (Simulation && saveLog) File.AppendAllText(LogFile, LogContent.ToString());
            if (Simulation && string.IsNullOrEmpty(SimulationFile) && saveLog) Process.Start(LogFile);
            if (!string.IsNullOrEmpty(SimulationFile))
            {
                simulationFileMutext.WaitOne();
                try
                {
                    var dictionary = SimulationFileFields.Length == 0 ? this.GetConfigValues() : this.GetConfigValues().Where(p => SimulationFileFields.Contains(p.Key)).Select(p => p);
                    var sb = new StringBuilder();
                    if (UserPortfolioList.Count > 0)
                    {
                        var ul = UserPortfolioList.First().Value;
                        sb.Append($"{ul.SideStr}\t{ul.Quantity}\t{ul.AvgCost}\t{ul.Total}\t{ul.PL}\t{ul.Commission}\t{ul.PL - ul.Commission}\t{orderCounter}\t{LogFile}\t");
                    }
                    foreach (var v in dictionary) sb.Append(v.Value + "\t");
                    sb.Append(Environment.NewLine);
                    File.AppendAllText(SimulationFile, sb.ToString());
                }
                finally
                {
                    simulationFileMutext.ReleaseMutex();
                }
            }

        }


        public virtual void ClosePositions(string symbol, SignalResult signalResult)
        {

        }

        public virtual void sendOrder(string symbol, decimal quantity, BuySell side, string comment = "", decimal lprice = 0, OrderIcon icon = OrderIcon.None, DateTime? t = null, SignalResult signalResult = null, bool disableDelay = false)
        {
            orderWait.Reset();
            var monitored = this.Monitor.Dump(true).ToString();
            if (!string.IsNullOrEmpty(monitored)) Log($"\n*** ORDER DATA ***\n{monitored}\n******", LogLevel.Debug, t);
            var symbolData = GetSymbolData(symbol, this.SymbolPeriod);
            var portfolio = UserPortfolioList.GetPortfolio(symbol);
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
            portfolio.AddRequestedOrder(order);
            Log($"New order submitted. Market price was: {price}: {this.positionRequest.ToString()}", LogLevel.Info, t);
            if (order.SignalResult != null)
                Log($"Signal [{order.SignalResult.Signal.Name}] result: {order.SignalResult}", LogLevel.Info, t);
            Log($"Used bar: {symbolData.Periods.Last}", LogLevel.Debug, t);

            if (this.UseVirtualOrders)
            {
                if (this.Simulation && !disableDelay)
                {
                    var algoTime = Now;
                    //this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = AlgoTime.AddSeconds(0.5 + new RandomGenerator().NextDouble() * 2) };
                    this.delayedOrder = new DelayedOrder() { created = algoTime, order = positionRequest, scheduled2 = Now.AddSeconds(2) };
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
        public ConcurrentDictionary<string, SignalResult> SignalResults = new ConcurrentDictionary<string, SignalResult>();


        //public DateTime AlgoTime
        //{
        //    get
        //    {
        //        return TimeSet ?? DateTime.Now;
        //    }
        //    set
        //    {
        //        TimeSet = value;
        //    }
        //}

        public string LogFile
        {
            get
            {
                var inner = this.Simulation ? "simulation" : "live";
                return Path.Combine(LogDir, inner, $"{InstanceName}.txt");
            }
        }

        public double CalculateVolumeBySecond(DateTime t, decimal volume)
        {
            Helper.SymbolSeconds(SymbolPeriod.ToString(), out int periodSeconds);
            var rounded = Helper.RoundDown(t, TimeSpan.FromSeconds(periodSeconds));
            var elapsedSeconds = Math.Max(1, (t - rounded).TotalSeconds);
            if (elapsedSeconds > 15) return (double)volume / elapsedSeconds;
            else return 0;
        }

        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            var ilevel = (int)level;
            if (ilevel >= (int)this.LoggingLevel)
            {
                var time = t ?? Now;
                string opTime = time.ToString("yyyy.MM.dd HH:mm:sss");
                var content = $"[{level}:{opTime}]: {text}";
                if (Simulation) LogContent.AppendLine(content);
                else
                {
                    lock(this)
                    {
                        File.AppendAllText(LogFile, content + Environment.NewLine);
                    }                    
                }
                var showUser = ilevel >= (int)LogLevel.Debug;
                if (LogConsole && showUser) Console.WriteLine(content);
                if (Exchange != null && showUser) Exchange.Log(content, level, t);
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
            lock (simulationLock)
            {
                var values = PriceLogger.GetMarketDataList(t ?? DateTime.Now);
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
        }

        public virtual decimal GetVolume(string symbol, BarPeriod period, DateTime? t = null)
        {
            if (Simulation)
            {


                var list = GetMarketData(symbol, SymbolPeriod, t);
                var vol = list.Length > 0 ? list[1] : 0;
                return vol;

            }
            else return Exchange.GetVolume(symbol, period, t);
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            if (Simulation)
            {
                var list = GetMarketData(symbol, SymbolPeriod, t ?? Now);
                var price = list.Length > 0 ? list[0] : 0;
                return price;
            }
            else return Exchange.GetMarketPrice(symbol, t);
        }


    }
}
