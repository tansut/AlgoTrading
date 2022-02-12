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

namespace Kalitte.Trading.Matrix
{
    public class AlgoBase : MatriksAlgo, IDisposable, ILogProvider
    {

        [Parameter(0)]
        public int LoggingLevel { get; set; }

        [Parameter(false)]
        public bool Simulation = false;

        public string LogDir = @"c:\kalitte\log";
        public MarketDataFileLogger PriceLogger;

        public string InstanceName { get; set; }

        private FileStream logStream;


        public PortfolioList UserPortfolioList = new PortfolioList();

        private static Dictionary<SymbolPeriod, int> symbolPeriodCache = new Dictionary<SymbolPeriod, int>();

        public AlgoBase()
        {
            RandomGenerator random = new RandomGenerator();
            if (!Directory.Exists(Path.GetDirectoryName(LogFile))) Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
            this.InstanceName = this.GetType().Name + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + (random.Next(1000000, 9999999));
            //logStream = new FileStream(LogFile, FileMode.Create, FileAccess.Write, FileShare.Read, 4096);
            
        }

        public string LogFile {
            get {
                return Path.Combine(LogDir, $"algologs2{(Simulation ? 'B' : 'L')}", $" {InstanceName}.txt");
            }
        }

        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            if ((int)level >= this.LoggingLevel)
            {
                string opTime = t.HasValue ? t.Value.ToString("yyyy.MM.dd HH:mm:sss") + "*" : DateTime.Now.ToString("yyyy.MM.dd HH:mm:sss");
                var content = $"[{level}:{opTime}]: {text}" + Environment.NewLine;

                Debug(content);
                var file = LogFile;
                
                //var bytes = Encoding.UTF8.GetBytes(content);
                //logStream.Write(bytes, 0, bytes.Length);
                File.AppendAllText(file, content + Environment.NewLine);
                
            }
        }

        public override void Dispose()
        {
            //logStream.Dispose();
            base.Dispose();
        }

        static AlgoBase()
        {
            symbolPeriodCache.Add(SymbolPeriod.Min, 60);
            symbolPeriodCache.Add(SymbolPeriod.Min5, 5 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min10, 10 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min15, 15 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min20, 20 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min30, 30 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min60, 60 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min120, 120 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min180, 180 * 60);
            symbolPeriodCache.Add(SymbolPeriod.Min240, 180 * 60);
        }

        public List<Signal> signals = new List<Signal>();
        public ConcurrentDictionary<string, SignalResultX> signalResults = new ConcurrentDictionary<string, SignalResultX>();

        public int GetSymbolPeriodSeconds(SymbolPeriod period)
        {
            int result;
            symbolPeriodCache.TryGetValue(period, out result);
            if (result == 0) throw new ArgumentException("Not supported period");
            return result;
        }

        public virtual bool CrossAboveX(IIndicator i1, IIndicator i2, DateTime? t = null)
        {
            return this.CrossAbove(i1, i2);
        }

        public virtual bool CrossBelowX(IIndicator i1, IIndicator i2, DateTime? t = null)
        {
            return this.CrossBelow(i1, i2);
        }

        public void LoadRealPositions(string symbol)
        {
            var positions = Simulation ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            UserPortfolioList.LoadRealPositions(positions, p => p.Symbol == symbol);
            Log($"- PORTFOLIO -");
            if (UserPortfolioList.Count > 0) Log($"{UserPortfolioList.Print()}");
            else Log("!! Portfolio is empty !!");
            Log($"- END PORTFOLIO -");
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            if (Simulation)
            {
                var price = PriceLogger.GetMarketData(t.Value) ?? 0;
                if (price ==0)
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
            } else return this.GetMarketData(symbol, SymbolUpdateField.Last);
            
        }

    }

}
