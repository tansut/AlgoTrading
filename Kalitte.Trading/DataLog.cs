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
using System.Globalization;
using Skender.Stock.Indicators;
using Kalitte.Trading.Matrix;

namespace Kalitte.Trading
{
    public abstract class MarketDataLogger : IDisposable
    {
        public char Seperator = '\t';
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
        public virtual decimal? GetMarketData(DateTime t)
        {
            var res = this.GetMarketDataList(t);
            return (res != null && res.Length > 0) ? res[0] : default(decimal?);
        }
        public abstract decimal[] GetMarketDataList(DateTime t);
    }

    public abstract class FileLogger : MarketDataLogger
    {
        public string Dir
        {
            get; set;
        }
        public bool SaveDaily = false;
        public string FileName { get; set; }
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
            if (!string.IsNullOrEmpty(FileName)) return Path.Combine(usedDir, FileName);
            if (SaveDaily)
                return Path.Combine(usedDir, t.ToString("yyyy-MM-dd") + ".txt");
            else return Path.Combine(usedDir, t.ToString("yyyy-MM-dd"), t.ToString("HH")) + ".txt";
        }

        public override void LogMarketData(DateTime t, decimal[] price)
        {
            string append = $"{t.ToString("yyyy.MM.dd HH:mm:ss")}{Seperator}{string.Join(Convert.ToString(Seperator), price.Select(p=>p.ToString(CultureInfo.InvariantCulture)))}\n";
            string file = GetFileName(t);
            if (!Directory.Exists(Path.GetDirectoryName(file))) Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.AppendAllText(file, append);
        }

        public List<decimal[]> GetContentValues(string file)
        {
            
            var content = new List< decimal[]>();
            if (File.Exists(file))
            {
                var fileContent = File.ReadAllLines(file);
                foreach (var line in fileContent)
                {
                    var parts = new List<string>(line.Split(Seperator));
                    try
                    {
                        var key = parts[0];
                        parts.RemoveAt(0);
                        content.Add(parts.Select(p => decimal.Parse(p, CultureInfo.InvariantCulture)).ToArray());
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            
            return content;
        }

        public SortedList<string, decimal[]> GetContent(string file)
        {
           
            var cacheContains = cache.ContainsKey(file);
            var content = cacheContains ? cache[file] : new SortedList<string, decimal[]>();
            if (!content.Any() && File.Exists(file))
            {
                var fileContent = File.ReadAllLines(file);
                foreach (var line in fileContent)
                {
                    var parts = new List<string>(line.Split(Seperator));
                    try
                    {
                        var key = parts[0];
                        parts.RemoveAt(0);
                        try
                        {
                            content.Add(key, parts.Select(p => decimal.Parse(p, CultureInfo.InvariantCulture)).ToArray());
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            if (!cacheContains) cache[file] = content;
            return content;
        }


        public override decimal[] GetMarketDataList(DateTime t)
        {
            var file = GetFileName(t);
            var content = GetContent(file);
            decimal[] result;
            var f1 = content.TryGetValue(t.ToString("yyyy.MM.dd HH:mm:sss"), out result);
            if (!f1) content.TryGetValue(t.ToString("mm-ss"), out result);
            return result == null ? new decimal[0]: result;
        }
    }

    public class MarketDataFileLogger : FileLogger
    {

        public FinanceBars GetContentAsQuote(string symbol, BarPeriod period, DateTime t)
        {
            var file = GetFileName(t);
            var content = GetContent(file);
            var result = new FinanceBars(symbol, period);


            foreach (var line in content)
            {
                var date = DateTime.Parse(line.Key, CultureInfo.InvariantCulture);
                if (date > t)
                {
                    //AlgoBase.Current.Log($"{date} is greater than {t} ");
                    return result;
                }
                var q = new MyQuote()
                {
                    Date = date,
                    Open = line.Value[0],
                    High = line.Value[1],
                    Low = line.Value[2],
                    Close = line.Value[3],
                    //WClose = line.Value[4],
                    //Quantity = line.Value[5],
                    Volume = line.Value[5],
                    //Ema5 = line.Value[9],
                    //Ema9 = line.Value[10],
                    //Rsi = line.Value[11],
                    //Macd59 = line.Value[12],
                    //Macd59t = line.Value[13]
                };
                result.Push(q);
            }
            return result;
        }



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
