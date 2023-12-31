﻿// algo
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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




    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }

    public class PotfolioConfig: ConfigParameters
    {
        [AlgoParam(0)]
        public decimal Quantity { get; set; }

        [AlgoParam(BuySell.Sell)]
        public BuySell Side { get; set; }

        [AlgoParam(0)]
        public decimal AvgCost { get; set; }
    }

    [Serializable]
    public class SymbolData
    {
        public string Symbol { get; private set; }
        public FinanceBars Periods { get; private set; }


        public SymbolData(string symbol, FinanceBars periods)
        {
            Symbol = symbol;
            Periods = periods;
        }
    }

    public class SignalData
    {
        public DateTime time;
        public SignalBase signal;

        internal SignalData(DateTime time, SignalBase signal)
        {
            this.time = time;
            this.signal = signal;
        }
    }

    public class StateSettings
    {
        public decimal LastCost { get; set; }
        public decimal LastQuantity { get; set; }
        public string LastSignal { get; set; }
    }

    public class OrderEventArgs: EventArgs
    {
        public ExchangeOrder Order { get; set; }
        public PortfolioItem Portfolio { get; set; }
    }     
    
    public class OrderDayConfig
    {
        [AlgoParam(1)]
        public decimal Rate { get; set; } = 1;

        public decimal H00 { get; set; } = 1;
        public decimal H01 { get; set; } = 1;
        public decimal H02 { get; set; } = 1;
        public decimal H03 { get; set; } = 1;
        public decimal H04 { get; set; } = 1;
        public decimal H05 { get; set; } = 1;
        public decimal H06 { get; set; } = 1;
        public decimal H07 { get; set; } = 1;
        public decimal H08 { get; set; } = 1;
        public decimal H09 { get; set; } = 1;
        public decimal H10 { get; set; } = 1;
        public decimal H11 { get; set; } = 1;
        public decimal H12 { get; set; } = 1;
        public decimal H13 { get; set; } = 1;
        public decimal H14 { get; set; } = 1;
        public decimal H15 { get; set; } = 1;
        public decimal H16 { get; set; } = 1;
        public decimal H17 { get; set; } = 1;
        public decimal H18 { get; set; } = 1;
        public decimal H19 { get; set; } = 1;
        public decimal H20 { get; set; } = 1;
        public decimal H21 { get; set; } = 1;
        public decimal H22 { get; set; } = 1;
        public decimal H23 { get; set; } = 1;
    }


    public abstract class AlgoBase : ILogProvider
    {
        private object signalLock = new object();
        private object decideLock = new object();
        private object orderLock = new object();


        

        private static volatile int AlgoCounter = 0;

        public int UniqueID { get; private set; }

        public event EventHandler<OrderEventArgs> OrderCompleted;

        Mutex simulationFileMutext = new Mutex(false, "simulationFileMutext2");

        Dictionary<string, MarketDataFileLogger> dataProviders = new Dictionary<string, MarketDataFileLogger>();
        
        [AlgoParam(null, "Portfolio")]
        public PotfolioConfig InitialPortfolio { get; set; }

        public List<SymbolData> Symbols { get; set; } = new List<SymbolData>();

        public VolatileRatio VolatileRatio { get; set; } = VolatileRatio.Average;

        public IExchange Exchange { get; set; }

        public bool MultipleTestOptimization { get; set; } = false;
        public DateTime? TestStart { get; set; }
        public DateTime? TestFinish { get; set; }
        public string SimulationFile { get; set; } = "";
        public string[] SimulationFileFields { get; set; } = new string[] { };

        private DateTime? time { get; set; } = null;
        private ReaderWriterLock timeReaderLock = new ReaderWriterLock();

        private object simulationLock = new object();

        [AlgoParam(LogLevel.Verbose)]
        public LogLevel LoggingLevel { get; set; }

        [AlgoParam(LogLevel.Info)]
        public LogLevel UILoggingLevel { get; set; }

        [AlgoParam(false)]
        public bool Simulation { get; set; }

        [AlgoParam(true)]
        public bool LogConsole { get; set; }

        public StateSettings LoadStateSettings()
        {
            var file = Path.Combine(LogDir, Symbol, $"state{(MultipleTestOptimization ? UniqueID.ToString() : "")}.json");
            if (File.Exists(file)) {
                return JsonConvert.DeserializeObject<StateSettings>(File.ReadAllText(file));
            } else return new StateSettings();
        }

        public void SaveStateSettings(StateSettings settings)
        {
            var content =JsonConvert.SerializeObject(settings);
            var file = Path.Combine(LogDir, Symbol, $"state{(MultipleTestOptimization ? UniqueID.ToString() : "")}.json");
            File.WriteAllText(file, content);
        }

        public string LogDir { get
            {
                return Path.Combine(Appdir, "log");
            }
        }

        [AlgoParam(@"c:\kalitte")]
        public string Appdir { get; set; }

        public MarketDataFileLogger PriceLogger;
        public string InstanceName { get; set; }

        [AlgoParam("F_XU0300422")]
        public string Symbol { get; set; } = "F_XU0300422";


        [AlgoParam(false)]
        public bool UsePerformanceMonitor { get; set; }


        [AlgoParam(BarPeriod.Min10)]
        public BarPeriod SymbolPeriod { get; set; }


        [AlgoParam(false)]
        public bool UseVirtualOrders { get; set; }


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
            timeReaderLock.AcquireWriterLock(1000);
            try
            {
                return (DateTime)(time = t);
            }
            finally
            {
                timeReaderLock.ReleaseWriterLock();
            }

        }

        public DateTime Now
        {
            get
            {
                //timeReaderLock.AcquireReaderLock(1000);
                try
                {
                    return time ?? DateTime.Now;
                }
                finally
                {
                    //timeReaderLock.ReleaseReaderLock();
                }

            }
        }

        public virtual void InitializePositions(List<PortfolioItem> portfolioItems, bool keepPortfolio = false)
        {
            var state = LoadStateSettings();
            var portfolio = portfolioItems.FirstOrDefault(p => p.Symbol == Symbol);
            if (!string.IsNullOrEmpty(state.LastSignal) && portfolio != null)
            {
                if (state.LastQuantity != portfolio.Quantity) throw new Exception($"Portfolio quantity seems {portfolio.Quantity}, last time it was {state.LastQuantity}. Update state file or reload Algo.");
                var signal = this.Signals.FirstOrDefault(p => p.Name == state.LastSignal);
                if (signal == null) throw new Exception("Unknown signal in state file");
                var order = new ExchangeOrder(this.Symbol, "-1", portfolio.Side, portfolio.Quantity, state.LastCost, "Loaded from file", Now);
                order.ExtenallyLoaded = true;
                order.FilledQuantity = order.Quantity;
                order.FilledUnitPrice = order.UnitPrice;
                order.Usage = OrderUsage.CreatePosition;
                order.SignalResult = new SignalResult(signal, Now);
                order.SignalResult.finalResult = order.Side;
                UserPortfolioList.OrderCompleted(order, keepPortfolio);
            }
            else portfolioItems.ForEach(p => this.UserPortfolioList[p.Symbol] = p);
        }

        public PerformanceMonitor Watch;
        int orderCounter = 0;

        public Dictionary<string, decimal> ordersBySignals = new Dictionary<string, decimal>();

        private Func<object, SignalResult> signalAction = (object stateo) =>
        {
            var state = (SignalData)stateo;
            return state.signal.Check(state.time);
        };

        internal Task[] RunSignals(DateTime time)
        {
            var tasks = new List<Task<SignalResult>>();
            foreach (var signal in Signals)
            {
                var sd = new SignalData(time, signal);
                tasks.Add(Task<SignalResult>.Factory.StartNew(this.signalAction, sd));
            }
            var result = tasks.ToArray();
            Task.WaitAll(result);
            return result;

        }

        public Dictionary<string, PerformanceCounter> perfCounters = new Dictionary<string, PerformanceCounter>();


        int virtualOrderCounter = 0;
        public ExchangeOrder positionRequest = null;
        public int simulationCount = 0;
        public StartableState SignalsState { get; private set; } = StartableState.Stopped;

        public ManualResetEvent orderWait = new ManualResetEvent(true);

        public abstract void Decide(SignalBase signal, SignalEventArgs data);


        public virtual void CheckBacktestAfterRun(DateTime t)
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

        public virtual void CheckBacktestBeforeRun(DateTime t)
        {
            //var stats = this.UserPortfolioList.GetPortfolio(this.Symbol).GetDailyStats(t);
            //if (stats.Total <= 0) DayStart();
        }

        //public bool OrderInProgress
        //{
        //    get
        //    {
        //        bool taken = false;
        //        try
        //        {
        //            Monitor.TryEnter(orderLock, ref taken);
        //            return !taken;
        //        } finally
        //        {
        //            if (taken) Monitor.Exit(orderLock);     
        //        }
        //    }
        //}

        public virtual void ConfigureMonitor()
        {
            this.Watch.DefaultChange = 25.0M;
            this.Watch.MonitorEvent += Monitor_MonitorEvent;
        }

        protected virtual void Monitor_MonitorEvent(object sender, MonitorEventArgs e)
        {
            if (e.EventType == MonitorEventType.Updated)
                Log($"{e}", LogLevel.Debug);
        }

        public virtual void CompletedOrder(ExchangeOrder order)
        {

        }

        public virtual void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            var savePosition = this.positionRequest;
            try
            {
                if (savePosition == null)
                {
                    Log("Received filled order signal but probably current order was cancelled before.", LogLevel.Warning);
                }
                else
                {
                    this.positionRequest.FilledUnitPrice = filledUnitPrice;
                    this.positionRequest.FilledQuantity = filledQuantity;
                    var portfolio = this.UserPortfolioList.OrderCompleted(this.positionRequest);
                    var port = UserPortfolioList.GetPortfolio(positionRequest.Symbol);
                    var stat = port.GetDailyStats(positionRequest.Sent);
                    Log($"Filled[{port.SideStr}/{port.Quantity}/{port.AvgCost.ToCurrency()} NetPL:{stat.NetPl.ToCurrency()}/{port.NetPL.ToCurrency()}]: {this.positionRequest.ToString()}", LogLevel.Order);
                    CountOrder(this.positionRequest.SignalResult.Signal.Name, filledQuantity);
                    if (this.positionRequest.Usage == OrderUsage.CreatePosition)
                    {
                        var newState = new StateSettings();
                        newState.LastSignal = this.positionRequest.SignalResult.Signal.Name;
                        var avgCost = portfolio.LastAverageCost(this.positionRequest.SignalResult.Signal);
                        newState.LastCost = avgCost.AverageCost;
                        newState.LastQuantity = portfolio.Quantity;
                        SaveStateSettings(newState);
                    } else 
                    {
                        var state = LoadStateSettings();
                        if (!string.IsNullOrEmpty(state.LastSignal))
                        {
                            var avgCost = portfolio.LastAverageCost(this.positionRequest.SignalResult.Signal);
                            state.LastQuantity = portfolio.Quantity;
                            state.LastCost = avgCost.AverageCost;
                            SaveStateSettings(state);
                        }
                    }
                    this.positionRequest = null;
                    orderCounter++;                    
                    CompletedOrder(savePosition);
                    if (OrderCompleted != null) OrderCompleted(this, new OrderEventArgs()
                    {
                        Order = savePosition,
                        Portfolio = portfolio
                    });
                    
                }
            }
            finally
            {
                orderWait.Set();
            }
        }

        public decimal RoundQuantity(decimal quantity)
        {
            var q = quantity > 0 && quantity < 1 ? 1 : quantity;
            return Math.Round(q);
        }

        public virtual void CancelCurrentOrder(string reason)
        {
            Log($"Order rejected/cancelled [{reason}]", LogLevel.Warning);
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
                shouldBe = t.Date.AddHours(18).AddMinutes(seconds > 600 ? 0: seconds / 60);
            }
            return shouldBe;
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

        public void StartSignals(params SignalBase[] signals)
        {
            SignalsState = StartableState.StartInProgress;
            try
            {
                var list = signals.Length == 0 ? Signals.ToArray() : signals;
                foreach (var signal in list)
                {
                    //if (!signal.Enabled) continue;
                    signal.Start();
                    Log($"Started signal {signal}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting signals: {ex.Message}, {ex.Message}", LogLevel.Error);
            }

            SignalsState = StartableState.Started; 
        }

        public virtual void StopSignals(params SignalBase[] signals)
        {
            SignalsState = StartableState.StopInProgress;
            try
            {
                var list = signals.Length == 0 ? Signals.ToArray() : signals;
                foreach (var signal in list)
                {
                    //if (!signal.Enabled) continue;
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
            return time.Hour == 9 && time.Minute <= 31;
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
            var t4 = new DateTime(t.Year, t.Month, t.Day, 22, 55, 0);
            try
            {
                if ((t >= t1 && t <= t2) || (t >= t3 && t <= t4))
                {
                    //SetBarCurrentValues();
                    if (SignalsState == StartableState.Stopped)
                    {
                        Log($"Time seems OK, starting signals ...");
                        if (IsMorningStart(t))
                        {
                            DayStart();                            
                        }
                        StartSignals();
                    }
                }
                else
                {
                    if (SignalsState == StartableState.Started)
                    {
                        Log($"Time seems OK, stopping signals ...");
                        StopSignals(Signals.Where(p=> !(p is ClosePositionsSignal)).ToArray());
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



        public virtual void InitializeBars(string symbol, BarPeriod period, DateTime? t = null)
        {
            var periodBars = GetPeriodBars(symbol, period, t);
            this.Symbols.Add(new SymbolData(symbol, periodBars));
            Log($"Initialized total {periodBars.Count} for {symbol}/{period} using time {t}. Last bar is: {periodBars.Last}", LogLevel.Debug, t);
        }

        public virtual void DayStart()
        {

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
                Log($"Error initializing bars {ex.Message}/{ex.StackTrace}", LogLevel.Error, t);
            }

            return periodBars;

        }

        public AlgoBase() : this(null)
        {

        }



        public AlgoBase(Dictionary<string, object> initValues)
        {
            this.UniqueID = Interlocked.Increment(ref AlgoBase.AlgoCounter);
            this.ApplyProperties(initValues);
            RandomGenerator random = new RandomGenerator();
            this.InstanceName = this.GetType().Name + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + (random.Next(1000000, 9999999));
            
        }

        public Boolean WaitForOrder(string message)
        {
            var wait = Simulation ? 1 : 100;
            var result2 = orderWait.WaitOne(wait);
            if (!result2 && !Simulation) Log($"Waiting for last order to complete: {message}", LogLevel.Warning);
            return result2;
        }

        public bool WaitingOrderExpired()
        {
            if (orderWait.WaitOne(1)) return false;
            if (this.positionRequest == null) return false;
            var timeOut = (Now - this.positionRequest.LastUpdate).TotalSeconds;
            return timeOut > 30;
        }

        public void SignalReceieved(SignalBase signal, SignalEventArgs data)
        {
            SignalResult existing;
            BuySell? oldFinalResult = null;
            int? oldHashCode = null;
            lock (signalLock)
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
                if (data.Result.finalResult.HasValue || data.Result.preResult.HasValue)
                {
                    Monitor.Enter(decideLock);
                    try
                    {
                        Decide(signal, data);
                    }
                    finally
                    {
                        Monitor.Exit(decideLock);
                    }
                }
            }
        }
        public Dictionary<string, object> GetConfigValues()
        {
            return this.GetConfigValues(this);
        }

        public Dictionary<string, object> GetConfigValues(object target)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            var properties = target.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
            foreach (var prop in properties)
            {
                var attr = (AlgoParam)prop.GetCustomAttributes(true).Where(p => p is AlgoParam).First();
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsArray)
                {
                    var subClassProps = GetConfigValues(prop.GetValue(target));
                    foreach (var subClassProp in subClassProps) result.Add($"{attr.Name ?? prop.Name}/{subClassProp.Key}", subClassProp.Value);
                }
                else result.Add(attr.Name ?? prop.Name, prop.GetValue(target));


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
                if (prop.PropertyType.IsClass && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                {
                    var subClassProps = GetConfigValues(prop.PropertyType);
                    foreach (var subClassProp in subClassProps) result.Add($"{attr.Name ?? prop.Name}/{subClassProp.Key}", subClassProp.Value);
                }
                else result.Add(attr.Name ?? prop.Name, attr.Value);
            }
            return result;
        }

        public void ApplyProperties(object target, Dictionary<string, object> init = null)
        {
            if (init == null) init = GetConfigValues(target.GetType());
            var properties = target.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
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
                        if (item.PropertyType == typeof(string))
                        {
                            propValue = val;
                        }
                        else if (item.PropertyType == typeof(int))
                        {
                            propValue = Convert.ToInt32(val);
                        }
                        else if (item.PropertyType.IsEnum)
                        {
                            propValue = Enum.ToObject(item.PropertyType, Convert.ToInt32(val));
                        }
                        else if (item.PropertyType == typeof(BarPeriod))
                        {
                            propValue = (BarPeriod)Convert.ToInt32(val);
                        } else if (item.PropertyType.IsArray)
                        {
                            var jarr = val as Newtonsoft.Json.Linq.JArray;
                            if (jarr != null)
                            {
                                var list = new List<object>();
                                foreach (var jitem in jarr)
                                {
                                    var jval = (jitem as JValue).Value;

                                    if (jval.GetType() == typeof(double)) jval = Convert.ToDecimal(jval);
                                    list.Add(jval);
                                }
                                propValue = Activator.CreateInstance(item.PropertyType, list.Count);
                                for(int i = 0; i < list.Count; i++) ((Array)propValue).SetValue(list[i], i);
                            }
                            else propValue = val;
                        }
                    }
                    try
                    {
                        target.GetType().GetProperty(item.Name).SetValue(target, propValue);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{item.Name} {propValue} set error", ex);

                    }
                }
                else
                {
                    var paramVal = item.GetCustomAttributes(typeof(AlgoParam), true).Cast<AlgoParam>().FirstOrDefault();

                    if (paramVal != null && (!item.PropertyType.IsClass || item.PropertyType.IsArray || item.PropertyType == typeof(string))) item.SetValue(target, paramVal.Value);
                    else if (paramVal != null)
                    {
                        var subProps = init.Where(p => p.Key.StartsWith($"{paramVal.Name ?? item.Name}/")).ToList();
                        var dict = new Dictionary<string, object>();
                        subProps.ForEach(p => dict.Add(p.Key.Replace(p.Key.Split('/')[0] + "/", ""), p.Value));
                        if (item.GetValue(target) == null)
                        {
                            try
                            {
                                var instance = Activator.CreateInstance(item.PropertyType);
                                item.SetValue(target, instance);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"{item.Name}/{item.PropertyType.FullName} property error while creating instance", ex);
                            }
                        }
                        if (subProps.Any()) ApplyProperties(item.GetValue(target), dict);
                    }
                }
            }
        }


        public void ApplyProperties(Dictionary<string, object> init = null)
        {
            ApplyProperties(this, init);
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
            if (Simulation && InitialPortfolio.Quantity > 0)
            {
                var item = new PortfolioItem(this.Symbol, InitialPortfolio.Side, InitialPortfolio.Quantity, InitialPortfolio.AvgCost);
                InitializePositions(new List<PortfolioItem> { item });                
            }
            if (this.UsePerformanceMonitor)
            {
                this.Watch = new PerformanceMonitor();
                ConfigureMonitor();
            }
        }



        public virtual void Start()
        {
            if (this.Watch != null) this.Watch.Start();
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
            if (this.Watch != null) this.Watch.Stop();

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
            if (TestStart.HasValue)
            {
                Log($"For dates {TestStart} - {TestFinish}", LogLevel.FinalResult);
                var portfolio = UserPortfolioList.GetPortfolio(Symbol);
                foreach (var day in portfolio.DailyStats)
                {
                    Log($"{day.Key.ToString("dd/MM/yyyy")}\t{day.Value.Total}\t{day.Value.NetPl.ToCurrency()}", LogLevel.FinalResult);
                }

                foreach (var day in portfolio.DayOfWeekStats)
                {
                    Log($"{day.Key}\t{day.Value.Total}\t{day.Value.NetPl.ToCurrency()}", LogLevel.FinalResult);
                }

                foreach (var day in portfolio.HourlyStats)
                {
                    Log($"{day.Key}\t{day.Value.Total}\t{day.Value.NetPl.ToCurrency()}", LogLevel.FinalResult);
                }
            }

            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;


            if (Simulation) File.AppendAllText(LogFile, LogContent.ToString());
            if (Simulation && string.IsNullOrEmpty(SimulationFile)) Process.Start(LogFile);
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
                    foreach (var v in dictionary)
                    {
                        if (v.Value != null && v.Value.GetType().IsArray)
                        {
                            sb.Append(Helper.ToString(v.Value) + "\t");
                        }
                        else 
                        sb.Append(Helper.ToString(v.Value) + "\t");
                    }
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

        public virtual void sendOrder(string symbol, decimal quantity, BuySell side, string comment, SignalResult signalResult, OrderUsage usage = OrderUsage.Unknown, bool disableDelay = false)
        {
            orderWait.Reset();
            var signal = signalResult.Signal;
            usage = usage == OrderUsage.Unknown ? signal.Usage : usage;
            var icon = OrderIcon.None;
            if (usage == OrderUsage.StopLoss) icon = OrderIcon.StopLoss;
            else if (usage == OrderUsage.TakeProfit) icon = OrderIcon.TakeProfit;
            else if (usage == OrderUsage.ClosePosition) icon = OrderIcon.PositionClose;

            var time = Now;
            if (this.Watch != null)
            {
                var monitored = this.Watch.Dump(true).ToString();
                if (!string.IsNullOrEmpty(monitored)) Log($"\n*** ORDER DATA ***\n{monitored}\n******", LogLevel.Debug, time);
            }
            var symbolData = GetSymbolData(symbol, this.SymbolPeriod);
            var portfolio = UserPortfolioList.GetPortfolio(symbol);
            var price = this.GetMarketPrice(symbol, time);
            if (price == 0)
            {
                Log($"Unable to get a marketprice at {time}, using close {symbolData.Periods.Last.Close} from {symbolData.Periods.Last}", LogLevel.Warning, time);
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
                orderid = Simulation ? Exchange.CreateMarketOrder(symbol, quantity, side, icon.ToString(), time.Hour >= 19) :
                    Exchange.CreateLimitOrder(symbol, quantity, side, limitPrice, icon.ToString(), time.Hour >= 19);
            }
            var order = this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment, time);
            order.Usage = usage;
            order.SignalResult = signalResult;
            order.Sent = time;
            order.LastUpdate = time;
            portfolio.AddRequestedOrder(order);
            Log($"New order submitted. Market price was: {price}: {this.positionRequest.ToString()}", LogLevel.Info, time);
            if (order.SignalResult != null)
                Log($"Signal [{order.SignalResult.Signal.Name}] result: {order.SignalResult}", LogLevel.Info, time);
            Log($"Used bar: {symbolData.Periods.Last}", LogLevel.Debug, time);

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



        public List<SignalBase> Signals = new List<SignalBase>();
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
                string opTime = level == LogLevel.FinalResult ? "" : $"[{ level}:{time.ToString("yyyy.MM.dd HH:mm:sss")}]: "; 
                var content = $"{opTime}{text}";
                if (Simulation && MultipleTestOptimization) LogContent.AppendLine(content);
                else
                {
                    lock (this)
                    {
                        File.AppendAllText(LogFile, content + Environment.NewLine);
                    }
                }
                var showUser = ilevel >= (int)UILoggingLevel;
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
            //, SymbolPeriod.Min10));
            //volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min15));
            //volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min20));
            //volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min30));
            //volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min60));
            //volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min120));


       

            decimal result;
            if (Simulation)
            {
                var list = GetMarketData(symbol, SymbolPeriod, t);
                result = list.Length > 0 ? list[(int)SymbolPeriod/10 - 2] : 0;
            }
            else result = Exchange.GetVolume(symbol, period, t);
            if (result == 0) Log($"Market volume got zero", LogLevel.Debug);
            return result;
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            var result = 0M;
            var time = t ?? Now;
            if (Simulation)
            {
                var list = GetMarketData(symbol, SymbolPeriod, time);
                result = list.Length > 0 ? list[0] : 0;

            }
            else result = Exchange.GetMarketPrice(symbol, time);
            if (result == 0) Log($"Market price got zero", LogLevel.Debug);
            return result;
        }


    }
}
