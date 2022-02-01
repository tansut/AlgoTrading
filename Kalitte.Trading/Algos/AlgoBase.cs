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
        private static Dictionary<SymbolPeriod, int> symbolPeriodCache = new Dictionary<SymbolPeriod, int>();
        [Parameter(1)]
        public int LoggingLevel { get; set; }
        public void Log(string text, LogLevel level = LogLevel.Info)
        {
            if ((int)level >= this.LoggingLevel) Debug(text);
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

        decimal i1val = 0.0M;


        public virtual bool CrossAboveX(IIndicator i1, IIndicator i2, DateTime? t = null)
        {
            return this.CrossAbove(i1, i2);
        }

        public virtual bool CrossBelowX(IIndicator i1, IIndicator i2, DateTime? t = null)
        {
            return this.CrossBelow(i1, i2);
        }

    }

}
