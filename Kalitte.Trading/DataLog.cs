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

namespace Kalitte.Trading
{
    public abstract class MarketDataLogger : IDisposable
    {
        public string Symbol
        {
            get; private set;
        }
        public virtual void Dispose()
        {

        }

        public MarketDataLogger(string symbol)
        {
            this.Symbol = symbol;
        }
        public virtual void LogMarketData(DateTime t, decimal price)
        {
            this.LogMarketData(t, new decimal[] { price });
        }
        public abstract void LogMarketData(DateTime t, decimal[] price);
        public virtual decimal GetMarketData(DateTime t)
        {
            var res = this.GetMarketDataList(t);
            return (res != null && res.Length > 0) ? res[0] : 0;
        }
        public abstract decimal[] GetMarketDataList(DateTime t);
    }

    public abstract class FileLogger : MarketDataLogger
    {
        public string Dir
        {
            get; set;
        }

        protected string usedDir;
        protected Dictionary<string, SortedList<string, decimal[]>> cache = new Dictionary<string, SortedList<string, decimal[]>>();
        public FileLogger(string symbol, string baseDir, string subdir) : base(symbol)
        {
            Dir = baseDir;
            usedDir = Path.Combine(Dir, Path.Combine(symbol, subdir));
            if (!Directory.Exists(usedDir)) Directory.CreateDirectory(usedDir);
        }

        public string GetFileName(DateTime t)
        {
            return Path.Combine(usedDir, t.ToString("yyyy-MM-dd"), t.ToString("HH")) + ".txt";
        }

        public override void LogMarketData(DateTime t, decimal[] price)
        {
            string append = $"{t.ToString("mm-ss")}\t{string.Join("\t", price)}\n";
            string file = GetFileName(t);
            if (!Directory.Exists(Path.GetDirectoryName(file))) Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.AppendAllText(file, append);
        }

        public override decimal[] GetMarketDataList(DateTime t)
        {
            var file = GetFileName(t);
            var cacheContains = cache.ContainsKey(file);
            var content = cacheContains ? cache[file] : new SortedList<string, decimal[]>();
            if (!content.Any() && File.Exists(file))
            {
                var fileContent = File.ReadAllLines(file);
                foreach (var line in fileContent)
                {
                    var parts = new List<string>(line.Split('\t'));
                    try
                    {
                        var key = parts[0];
                        parts.RemoveAt(0);
                        content.Add(key, parts.Select(p => decimal.Parse(p)).ToArray());
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            if (!cacheContains) cache[file] = content;
            decimal[] result;
            content.TryGetValue(t.ToString("mm-ss"), out result);
            return result;
        }
    }

    public class MarketDataFileLogger : FileLogger
    {


        public MarketDataFileLogger(string symbol, string baseDir, string type) : base(symbol, baseDir, type)
        {

        }





        //public void Convert()
        //      {
        //	var f = @"C:\kalitte\log\F_XU0300222\price\2022-01-28.txt";
        //	var lines = File.ReadAllLines(f);
        //	var dd = new DateTime(2022, 01, 28);
        //	foreach(var l in lines)
        //          {
        //		var parts = l.Split('\t');
        //		var h = parts[0].Split('-');
        //		var d = new DateTime(2022, 01, 28, int.Parse(h[0]), int.Parse(h[1]), int.Parse(h[2]));
        //		this.LogMarketPrice(d, decimal.Parse(parts[1]));
        //	}

        //}


    }

}
