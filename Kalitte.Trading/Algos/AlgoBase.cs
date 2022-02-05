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
    public class AlgoBase : MatriksAlgo
    {

        [Parameter(1)]
        public int LoggingLevel { get; set; }

        [Parameter(false)]
        public bool BackTestMode = false;

        public string LogDir = @"c:\kalitte\log";
        public MarketDataFileLogger PriceLogger;

        public string InstanceName { get; set; }


        public PortfolioList UserPortfolioList = new PortfolioList();

        private static Dictionary<SymbolPeriod, int> symbolPeriodCache = new Dictionary<SymbolPeriod, int>();

        public AlgoBase()
        {
            this.InstanceName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + (new Random().Next(10000));
        }

        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            if ((int)level >= this.LoggingLevel)
            {
                Debug(text);
                if (true)
                {


                    var file = Path.Combine(LogDir, $"algologs2{(BackTestMode ? 'B' : 'L')}", $" {InstanceName}.txt");
                    if (!Directory.Exists(Path.GetDirectoryName(file))) Directory.CreateDirectory(Path.GetDirectoryName(file));
                    string opTime = t.HasValue ? t.Value.ToString("yyyy.MM.dd HH:mm:sss") + "*" : "current";
                    File.AppendAllText(file, $"[{level}:{DateTime.Now.ToString("yyyy.MM.dd HH:mm:sss")}({opTime})]: {text}" + Environment.NewLine);

                }
            }
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
            var positions = BackTestMode ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            UserPortfolioList.LoadRealPositions(positions, p => p.Symbol == symbol);
            Log($"- PORTFOLIO -");
            if (UserPortfolioList.Count > 0) Log($"{UserPortfolioList.Print()}");
            else Log("!! Portfolio is empty !!");
            Log($"- END PORTFOLIO -");
        }

        public decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            if (BackTestMode) return PriceLogger.GetMarketData(t.HasValue ? t.Value : DateTime.Now);
            var price = this.GetMarketData(symbol, SymbolUpdateField.Last);
            return price;
        }

    }

}
