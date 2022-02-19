// algo
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    public class DelayedOrder
    {
        public ExchangeOrder order;
        public DateTime scheduled2;
        public DateTime created;
    }

    public class AlgoBase
    {
        public static AlgoBase Current;

        public IMarketDataProvider DataProvider { get; set; }

        public LogLevel LoggingLevel { get; set; } = LogLevel.Info;
        public bool Simulation { get; set; } = false;
        public string LogDir = @"c:\kalitte\log";
        public MarketDataFileLogger PriceLogger;
        public string InstanceName { get; set; }
        public string Symbol { get; set; }
        public BarPeriod SymbolPeriod { get; set; }

        protected DateTime? TimeSet = null;

        public PortfolioList UserPortfolioList = new PortfolioList();
        decimal simulationPriceDif = 0;
        private static Dictionary<string, int> symbolPeriodCache = new Dictionary<string, int>();

        private DelayedOrder delayedOrder = null;
        System.Timers.Timer seansTimer;

        public FinanceBars PeriodBars = null;        
        int orderCounter = 0;

        public Dictionary<string, decimal> ordersBySignals = new Dictionary<string, decimal>();

        int virtualOrderCounter = 0;
        ExchangeOrder positionRequest = null;
        int simulationCount = 0;
        public StartableState SignalsState { get; private set; } = StartableState.Stopped;

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

            return periodBars
            
        }


        public AlgoBase()
        {
            RandomGenerator random = new RandomGenerator();
            if (!Directory.Exists(Path.GetDirectoryName(LogFile))) Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
            this.InstanceName = this.GetType().Name + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + (random.Next(1000000, 9999999));
            Current = this;
        }


        public void Init()
        {
            this.PriceLogger = new MarketDataFileLogger(Symbol, LogDir, "price");
        }

        public void Stop()
        {
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

            var netPL = simulationPriceDif + UserPortfolioList.PL - UserPortfolioList.Comission;

            if (Simulation && ExpectedNetPl > 0 && netPL < ExpectedNetPl) File.Delete(Algo.LogFile);
            else if (Simulation) Process.Start(LogFile);
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
                    //Debug(content);
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
            else return DataProvider.GetMarketPrice(symbol, t);

        }


    }
}
