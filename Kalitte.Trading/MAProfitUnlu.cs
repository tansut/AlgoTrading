﻿using System;
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

namespace Kalitte.Trading
{

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2
    }

    public class ExchangeOrder
    {
        public string Symbol;
        public string Id;
        public OrderSide Side;
        public decimal UnitPrice;
        public decimal Quantity;
        public string Comment;
        public decimal FilledUnitPrice
        {
            get; set;
        }
        public decimal FilledQuantity
        {
            get; set;
        }


        public decimal Total
        {
            get
            {
                return FilledUnitPrice * FilledQuantity;
            }
        }

        public ExchangeOrder(string symbol, string id, OrderSide side, decimal quantity, decimal unitPrice, string comment = "")
        {
            this.Symbol = symbol;
            this.Id = id;
            this.Side = side;
            this.Quantity = quantity;
            this.UnitPrice = unitPrice;
            this.Comment = comment;
            this.FilledUnitPrice = 0;
        }

        public string SideStr
        {
            get
            {
                return this.Side == OrderSide.Buy ? "long" : "short";
            }
        }

        public override string ToString()
        {
            return $"{this.Symbol}:{this.SideStr}/{this.Quantity}:{this.FilledQuantity}/{this.UnitPrice}:{this.FilledUnitPrice} {this.Comment}";
        }

        public ExchangeOrder Clone()
        {
            var clone = new ExchangeOrder(this.Symbol, "", this.Side, this.Quantity, this.UnitPrice);
            clone.FilledUnitPrice = this.FilledUnitPrice;
            clone.FilledQuantity = this.FilledQuantity;
            return clone;
        }
    }

    public class PortfolioItem
    {

        public static PortfolioItem FromTraderPosition(AlgoTraderPosition p)
        {
            var item = new PortfolioItem(p.Symbol);
            item.LoadFromTraderPosition(p);
            return item;
        }

        public void LoadFromTraderPosition(AlgoTraderPosition p)
        {
            this.Symbol = p.Symbol;
            this.Side = p.Side.Obj == Matriks.Trader.Core.Fields.Side.Buy ? OrderSide.Buy : OrderSide.Sell;
            this.AvgCost = p.AvgCost;
            this.Quantity = Math.Abs(p.QtyNet);

        }

        public string Symbol
        {
            get; private set;
        }
        public decimal PL
        {
            get; private set;
        }
        public decimal AvgCost
        {
            get; private set;
        }
        public decimal Quantity
        {
            get; private set;
        }
        public OrderSide Side
        {
            get; set;
        }

        public bool IsLong
        {
            get
            {
                return this.Quantity > 0 && this.Side == OrderSide.Buy;
            }
        }

        public bool IsShort
        {
            get
            {
                return this.Quantity > 0 && this.Side == OrderSide.Sell;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.Quantity <= 0;
            }
        }

        public string SideStr
        {
            get
            {
                return this.Side == OrderSide.Buy ? "long" : "short";
            }
        }

        public decimal Total
        {
            get
            {
                return AvgCost * Quantity;
            }
        }

        public override string ToString()
        {
            return $"{this.Symbol}:{SideStr}/{Quantity}/Cost: {AvgCost} Total: {Total} PL: {PL}";
        }

        public PortfolioItem(string symbol, OrderSide side, decimal quantity, decimal unitPrice)
        {
            this.Symbol = symbol;
            this.Side = side;
            this.Quantity = quantity;
            this.AvgCost = unitPrice;
        }
        public PortfolioItem(string symbol) : this(symbol, OrderSide.Buy, 0, 0)
        {

        }

        public void OrderCompleted(ExchangeOrder position)
        {
            if (this.IsEmpty)
            {
                this.Side = position.Side;
                this.Quantity = position.FilledQuantity;
                this.AvgCost = position.FilledUnitPrice;
            }
            else
                if (this.Side == position.Side)
            {
                this.AvgCost = (this.Total + position.Total) / (this.Quantity + position.FilledQuantity);
                this.Quantity += position.FilledQuantity;

            }
            else
            {
                if (this.Quantity == position.FilledQuantity)
                {
                    this.AvgCost = 0;
                    this.Quantity = 0;
                }
                else if (this.Quantity > position.FilledQuantity)
                {
                    var delta = position.FilledQuantity;
                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
                    var profit = delta * direction * (position.FilledUnitPrice - this.AvgCost);
                    PL += profit;
                    this.Quantity -= position.FilledQuantity;
                    if (this.Quantity == 0)
                    {
                        this.AvgCost = 0;
                    }
                }
                else
                {
                    var delta = this.Quantity;
                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
                    var profit = delta * direction * (position.FilledUnitPrice - this.AvgCost);
                    PL += profit;
                    this.Side = position.Side;
                    this.Quantity = position.FilledQuantity - this.Quantity;
                    this.AvgCost = position.FilledUnitPrice;
                }
            }
        }
    }
    public class PortfolioList : Dictionary<string, PortfolioItem>
    {

        public PortfolioItem GetPortfolio(string symbol)
        {
            if (!this.ContainsKey(symbol)) this.Add(symbol, new PortfolioItem(symbol));
            return this[symbol];
        }

        public PortfolioList()
        {

        }




        public PortfolioItem Add(ExchangeOrder position)
        {
            var portfolio = this.GetPortfolio(position.Symbol);
            portfolio.OrderCompleted(position);
            return portfolio;
        }

        public StringBuilder Print()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this)
            {
                sb.AppendLine(item.Value.ToString());
            }
            return sb;
        }

        internal void LoadRealPositions(Dictionary<string, AlgoTraderPosition> positions, Func<AlgoTraderPosition, bool> filter)
        {
            this.Clear();
            foreach (var position in positions)
            {
                if (position.Value.IsSymbol)
                {
                    if (filter(position.Value))
                        this.Add(position.Key, PortfolioItem.FromTraderPosition(position.Value));
                }
            }
        }

        public PortfolioItem UpdateFromTrade(AlgoTraderPosition position)
        {
            var item = this.GetPortfolio(position.Symbol);
            item.LoadFromTraderPosition(position);
            return item;
        }
    }

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

    public abstract class Signal
    {
        public string Name { get; set; }
        public MatriksAlgo Owner { get; set; }
        public bool Enabled { get; set; }        

        public Signal(string name, MatriksAlgo owner, bool enabled)
        {
            Name = name;
            Owner = owner;
            Enabled = enabled;            
        }

        //public abstract bool Long();
        //public abstract bool Short();

        public abstract OrderSide? Check(bool ensuring);


    }

    public class CrossSignal: Signal
    {
        public IIndicator i1 = null;
        public IIndicator i2 = null;
        public CrossSignal(string name, MatriksAlgo owner, bool enabled, IIndicator i1, IIndicator i2) : base(name, owner, enabled)
        {
            this.i1 = i1;
            this.i2 = i2;
            this.Enabled = enabled;
        }

        public override OrderSide? Check(bool ensuring)
        {
            OrderSide? result = null;
            if (Owner.CrossAbove(i1, i2)) result = OrderSide.Buy;
            else if (Owner.CrossBelow(i1, i2)) result= OrderSide.Sell;
            return result;
        }        
    }

    public class FlipFlopSignal : Signal
    {
        public OrderSide Side { get; set; }

        public FlipFlopSignal(string name, MatriksAlgo owner, bool enabled, OrderSide side=OrderSide.Buy): base(name, owner, enabled)
        {
            this.Side = side;            
        }



        public override OrderSide? Check(bool ensuring)
        {            
            var result = this.Side;
            this.Side = result == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
            return ensuring ? Check(false): result;
        }
    }

    //public class ManualSignal : Signal
    //{
    //    public OrderSide? Side { get; set; }

    //    public ManualSignal(string name, MatriksAlgo owner, OrderSide side = OrderSide.Buy, bool enabled = true) : base(name, owner)
    //    {
    //        this.Side = side;
    //        this.Enabled = enabled;
    //    }


    //    public override OrderSide? Check()
    //    {
    //        return this.Side;
    //    }
    //}


    public class SignalResult
    {
        public List<Signal> Sells = new List<Signal>();
        public List<Signal> Buys = new List<Signal>();

        public OrderSide? FinalResult()
        {            
            if (Sells.Count > Buys.Count) return OrderSide.Sell;
            if (Buys.Count > Sells.Count) return OrderSide.Buy;
            return null;
        }

        public List<Signal> FinalSignals()
        {            
            if (Sells.Count > Buys.Count) return Sells;
            if (Buys.Count > Sells.Count) return Buys;
            return null;
        }
    }


}



namespace Kalitte.Trading.Algos
{

    public class KalitteAlgo : MatriksAlgo
    {
        private static Dictionary<SymbolPeriod, int> symbolPeriodCache = new Dictionary<SymbolPeriod, int>();
        [Parameter(1)]
        public int LoggingLevel { get; set; }
        public void Log(string text, LogLevel level = LogLevel.Info)
        {
            if ((int)level >= this.LoggingLevel) Debug(text);
        }

        static KalitteAlgo()
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

        public int GetSymbolPeriodSeconds(SymbolPeriod period)
        {

            int result;
            symbolPeriodCache.TryGetValue(period, out result);
            if (result == 0) throw new ArgumentException("Not supported period");
            return result;
        }

    }

    public class PriceLogger : KalitteAlgo
    {



        [Parameter(1)]
        public int LogSeconds = 1;

        [SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";


        public string logDir = @"c:\kalitte\log";
        private MarketDataFileLogger priceLogger;
        private MarketDataFileLogger rsiLogger;
        private MarketDataFileLogger ma59Logger;        
        private MarketDataFileLogger macd953Logger;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;

        SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

        int MovPeriod = 5;
        int MovPeriod2 = 9;

        int MACDLongPeriod = 9;
        int MACDShortPeriod = 5;

        int MACDTrigger = 3;


        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            AddSymbolMarketData(Symbol);
            SetTimerInterval(1);

            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);


            this.priceLogger = new MarketDataFileLogger(Symbol, logDir, "price");
            this.rsiLogger = new MarketDataFileLogger(Symbol, logDir, "rsi");
            this.ma59Logger = new MarketDataFileLogger(Symbol, logDir, "ma59");
            //this.ma9Logger = new MarketDataFileLogger(Symbol, logDir, "ma9");
            this.macd953Logger = new MarketDataFileLogger(Symbol, logDir, "macd953");
        }

        public override void OnTimer()
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);
            if (t >= t1 && t <= t2)
            {
                var price = GetMarketData(Symbol, SymbolUpdateField.Last);
                priceLogger.LogMarketData(DateTime.Now, price);
                rsiLogger.LogMarketData(DateTime.Now, new decimal[] { price, rsi.CurrentValue });
                ma59Logger.LogMarketData(DateTime.Now, new decimal[] { price, mov.CurrentValue, mov2.CurrentValue, CrossBelow(mov, mov2) ? 1:0, CrossAbove(mov, mov2) ? 1:0 });
                macd953Logger.LogMarketData(DateTime.Now, new decimal[] { price, macd.CurrentValue, macd.MacdTrigger.CurrentValue, CrossBelow(macd, macd.MacdTrigger) ? 1:0, CrossAbove(macd, macd.MacdTrigger) ? 1:0 });
            }
        }
    }

    public class MaProfit : KalitteAlgo
    {
        private ManualResetEvent orderWait = new ManualResetEvent(true);
        private ManualResetEvent operationWait = new ManualResetEvent(true);
        //private ManualResetEvent ensureSignalWait = new ManualResetEvent(true);

        private bool ensuringOrder = false;

        //[SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";

        //[Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

        [Parameter(2)]
        public decimal OrderQuantity = 2M;

        [Parameter(5)]
        public int MovPeriod = 5;

        [Parameter(9)]
        public int MovPeriod2 = 9;

        //[Parameter(true)]
        public bool DoublePositions = true;


        [Parameter(false)]
        public bool UseVirtualOrders = false;

        //[Parameter(false)]
        public bool AutoCompleteOrders = false;

        [Parameter(false)]
        public bool SimulateOrderSignal = false;

        [Parameter(1)]
        public decimal ProfitQuantity = 1;

        [Parameter(1)]
        public decimal LossQuantity = 1;

        [Parameter(9)]
        public decimal ProfitPuan = 9;

        [Parameter(18)]
        public decimal LossPuan = 18;

        //[Parameter(0)]
        public int RsiLong = 0;

        //[Parameter(0)]
        public int RsiShort = 0;

        [Parameter(0)]
        public int MACDLongPeriod = 0;

        [Parameter(5)]
        public int MACDShortPeriod = 5;

        [Parameter(3)]
        public int MACDTrigger = 3;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;

        public string logDir = @"c:\kalitte\log";
        private MarketDataFileLogger fileLogger;


        [Parameter(false)]
        public bool BackTestMode = false;

        int virtualOrderCounter = 0;

        decimal takeProfitTotal = 0;
        decimal stopLossTotal = 0;

        bool buy = true;


        PortfolioList portfolios = new PortfolioList();
        ExchangeOrder positionRequest = null;
        System.Timers.Timer orderTimer;




        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);

            this.signals.Add(new CrossSignal("MA59Cross", this, !SimulateOrderSignal && MovPeriod > 0, mov, mov2));
            this.signals.Add(new CrossSignal("MACD59Cross", this, !SimulateOrderSignal && MACDLongPeriod > 0, macd, macd.MacdTrigger));
            this.signals.Add(new FlipFlopSignal("FlipFlop", this,  SimulateOrderSignal, OrderSide.Buy));
            

            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            orderTimer = new System.Timers.Timer(1000);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !BackTestMode)
            {
                AddSymbolMarketData(Symbol);
                SetTimerInterval(1);
            }
            this.fileLogger = new MarketDataFileLogger(Symbol, logDir, "price");
        }

        public decimal GetMarketPrice(DateTime? t = null)
        {
            if (BackTestMode) return fileLogger.GetMarketData(t.HasValue ? t.Value : DateTime.Now);
            var price = this.GetMarketData(Symbol, SymbolUpdateField.Last);
            return price;
        }

        public override void OnRealPositionUpdate(AlgoTraderPosition position)
        {

            //if (position.Symbol == Symbol)
            //{
            //	lock (ordrLock)
            //	{
            //		portfolios.UpdateFromTrade(position);
            //		Log("Portfolio Updated");
            //		Log(portfolios.Print());
            //	}
            //}


            //Log($"sym: {position.Symbol} side:{position.Side} total:{position.TotalPosition} amount:{position.Amount} cost:{position.AvgCost} avail:{position.QtyAvailable} net:{position.QtyNet}");

        }

        public void LoadRealPositions()
        {
            var positions = BackTestMode ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            portfolios.LoadRealPositions(positions, p => p.Symbol == this.Symbol);
            Log($"- PORTFOLIO -");
            Log($"{portfolios.Print()}");
        }







        public override void OnInitCompleted()
        {
            var assembly = typeof(MaProfit).Assembly.GetName();
            Log($"Inited with {assembly.FullName}");
            LoadRealPositions();
            orderTimer.Elapsed += OnOrderTimerEvent;
            orderTimer.AutoReset = true;
            orderTimer.Enabled = true;
        }

        private void OnOrderTimerEvent(Object source, ElapsedEventArgs e)
        {
            if (BackTestMode) return;
            this.CreateOrders(null);

        }




        public bool ensureWaitingPositions()
        {
            if (this.positionRequest != null || this.ensuringOrder)
            {
                //Log($"active position waiting: {positionRequest.Id}/{positionRequest.Symbol}/{positionRequest.Side}/{positionRequest.Quantity}");

                return false;
            }
            else return true;

        }

        public OrderSide? ManageProfitLoss(decimal? marketPrice = null, DateTime? t = null)
        {
            OrderSide? result = null;
            if (!this.ensureWaitingPositions()) return result;
            operationWait.WaitOne();
            orderWait.WaitOne();
            operationWait.Reset();
            try
            {
                var portfolio = portfolios.GetPortfolio(Symbol);

                if (!portfolio.IsEmpty)
                {
                    var price = marketPrice.HasValue ? marketPrice.Value : GetMarketPrice(t);
                    var pl = price - portfolio.AvgCost;

                    if (price == 0)
                    {
                        Log($"ProfitLoss price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                    }
                    else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Buy) && (pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        takeProfitTotal += Math.Abs(pl);
                        Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, ProfitQuantity, OrderSide.Sell, $"take profit order, PL: {pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
                        result = OrderSide.Sell;
                    }
                    else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Sell) && (-pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        takeProfitTotal += Math.Abs(pl);
                        Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, ProfitQuantity, OrderSide.Buy, $"take profit order, PL: {-pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
                        result = OrderSide.Buy;
                    }
                    else if ((this.LossQuantity > 0) && (portfolio.Side == OrderSide.Buy) && (pl <= -this.LossPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        stopLossTotal += Math.Abs(pl);
                        Log($"Stoploss: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, LossQuantity, OrderSide.Sell, $"stop loss order, PL: {pl}, stopLossTotal: {stopLossTotal}", price, ChartIcon.StopLoss);
                        result = OrderSide.Sell;
                    }
                    else if ((this.LossQuantity > 0) && (portfolio.Side == OrderSide.Sell) && (pl >= this.LossPuan) && (portfolio.Quantity == this.OrderQuantity))
                    {
                        stopLossTotal += Math.Abs(pl);
                        Log($"Stoploss: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                        sendOrder(Symbol, LossQuantity, OrderSide.Buy, $"stop loss order, PL: {pl}, stopLossTotal: {stopLossTotal}", price, ChartIcon.StopLoss);
                        result = OrderSide.Buy;
                    }
                }
            }
            finally
            {
                operationWait.Set();
            }



            return result;

        }

        public override void OnTimer()
        {
            ManageProfitLoss();
        }



        protected ExchangeOrder sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0, ChartIcon icon = ChartIcon.None)
        {
            if (!this.ensureWaitingPositions())
            {
                Log("Bekleyen pozisyon varken yeni pozisyon gönderilemez");
                return null;
            }
            Log($"Order received: {symbol} {quantity} {side} {comment}", LogLevel.Debug);
            orderWait.Reset();
            var price = lprice > 0 ? lprice : GetMarketPrice();
            string orderid;
            decimal limitPrice = Math.Round((price + price * 0.02M * (side == OrderSide.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;


            if (UseVirtualOrders)
            {
                orderid = virtualOrderCounter++.ToString();
            }
            else
            {
                orderid = BackTestMode ? this.SendMarketOrder(symbol, quantity, side, icon) :
                this.SendLimitOrder(symbol, quantity, side, limitPrice, icon, DateTime.Now.Hour >= 19);
            }

            this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment);
            Log($"Order created, waiting to complete: {this.positionRequest.ToString()}");
            if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
            return this.positionRequest;
        }

        public SignalResult ensureSignal()
        {
            this.ensuringOrder = true;
            SignalResult check = null;
            try
            {
                check = CheckSignals(false);
                var result = check.FinalResult();

                if (result.HasValue  && !BackTestMode)
                {
                    var wait = 10;                    
                    Thread.Sleep(wait * 1000);
                    check = CheckSignals(true);
                    var newResult = check.FinalResult();

                    if (newResult != result)
                    {
                        Log($"Signals not confirmed.");
                    };
                }
            }
            finally
            {
                this.ensuringOrder = false;
            }
            return check;
        }


        //public bool ensureSignal(Func<BarDataCurrentValues, bool> func, BarDataCurrentValues values, string caption = "")
        //{
        //    this.ensuringOrder = true;
        //    var result = false;
        //    try
        //    {
        //        result = func(values);
        //        if (result && !BackTestMode)
        //        {
        //            var wait = 10;
        //            Log($"Waiting to confirm {caption} signal in {wait} seconds...");
        //            Thread.Sleep(wait * 1000);
        //            result = func(values);
        //            if (!result)
        //            {
        //                Log($"{caption} signal not confirmed.");
        //            };
        //        }
        //    }
        //    finally
        //    {
        //        this.ensuringOrder = false;
        //    }
        //    return result;
        //}

        //public bool buySignal(BarDataCurrentValues barDataCurrentValues)
        //{
        //    if (SimulateOrderSignal) return buy;
        //    var result = this.CheckSignals();
        //    return result.FinalResult() == OrderSide.Buy;

        //    //var maSignal = MovPeriod > 0 ? CrossAbove(mov, mov2) : true;
        //    //var rsiSignal = RsiLong > 0 ? CrossAbove(rsi, RsiLong) : true;
        //    //var macdSignal = MACDLongPeriod > 0 ? CrossAbove(macd, macd.MacdTrigger) : true;

        //    //return maSignal && rsiSignal && macdSignal;
        //}


        //public bool sellSignal(BarDataCurrentValues barDataCurrentValues)
        //{
        //    if (SimulateOrderSignal) return !buy;

        //    var maSignal = MovPeriod > 0 ? CrossBelow(mov, mov2) : true;
        //    var rsiSignal = RsiShort > 0 ? CrossBelow(rsi, RsiShort) : true;
        //    var macdSignal = MACDLongPeriod > 0 ? CrossBelow(macd, macd.MacdTrigger) : true;

        //    return maSignal && rsiSignal && macdSignal;
        //}


        public SignalResult CheckSignals(bool ensuring)
        {            
            var results = new SignalResult();
            foreach(var signal in this.signals)
            {
                if (signal.Enabled)
                {
                    var res = signal.Check(ensuring);
                    if (res == OrderSide.Buy) results.Buys.Add(signal);
                    else if (res == OrderSide.Sell) results.Sells.Add(signal);
                }
            }
           
            return results;
        }



        DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            if (!this.BackTestMode) return;
            var portfolio = portfolios.GetPortfolio(Symbol);
            if (ProfitQuantity > 0 && !portfolio.IsEmpty)
            {
                var t = barDataCurrentValues.LastUpdate.DTime;
                var periodsToBack = GetSymbolPeriodSeconds(SymbolPeriod);

                var start = t - TimeSpan.FromSeconds(periodsToBack - 1);
                Log($"Backtest starting to check take profit. Bar: {t}, start: {start}  seconds back: {periodsToBack}", LogLevel.Debug);
                for (var i = 0; i < periodsToBack; i++)
                {
                    var date = start.AddSeconds(i);
                    var price = GetMarketPrice(date);
                    if (price != 0)
                    {
                        this.ManageProfitLoss(price, date);
                    }
                }
            }
            this.CreateOrders(barDataCurrentValues);
        }

        public void CreateOrders(BarDataCurrentValues barDataCurrentValues)
        {
            if (!this.ensureWaitingPositions()) return;
            operationWait.WaitOne();
            orderWait.WaitOne();
            decimal doubleMultiplier = 1.0M;
            OrderSide? side = null;

            operationWait.Reset();
            try
            {
                var portfolio = this.portfolios.GetPortfolio(Symbol);
                
                var check = ensureSignal();
                var result = check.FinalResult();

                if (result == OrderSide.Buy)
                {                    
                    if (!portfolio.IsLong)
                    {
                        side = OrderSide.Buy;
                        if (this.DoublePositions)
                        {
                            if (portfolio.IsShort)
                            {
                                doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && (ProfitQuantity > 0)) ? 1.5M : 2.0M;
                            }
                        }
                    }
                }

                else if (result == OrderSide.Sell)
                {
                    if (!portfolio.IsShort)
                    {                        
                        side = OrderSide.Sell;
                        if (this.DoublePositions)
                        {
                            if (portfolio.IsLong)
                            {
                                doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && (ProfitQuantity > 0)) ? 1.5M : 2.0M;
                            }
                        }
                    }

                }
                if (side != null)
                {                    
                    sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value, "[" + string.Join(",", check.FinalSignals().Select(p => p.Name).ToArray()) + "]");
                }
            }
            finally
            {
                operationWait.Set();
            }

        }

        public void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
        {
            this.positionRequest.FilledUnitPrice = filledUnitPrice;
            this.positionRequest.FilledQuantity = filledQuantity;
            var portfolio = this.portfolios.Add(this.positionRequest);
            Log($"Completed order: {this.positionRequest.ToString()}");
            Log($"Portfolio: {portfolio.ToString()}");
            this.positionRequest = null;
            orderWait.Set();
        }

        public override void OnOrderUpdate(IOrder order)
        {
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                //Log($"fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}");
                if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
                {
                    if (BackTestMode)
                    {
                        this.FillCurrentOrder(order.Price, this.positionRequest.Quantity);
                    }
                    else
                    {
                        this.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                    }
                }
            }
        }



        public override void OnStopped()
        {
            if (orderTimer != null)
            {
                orderTimer.Stop();
            }
            Log($"Portfolio ended: {portfolios.Print()}");
        }
    }

}


namespace Matriks.Lean.Algotrader
{
    public class MaAlg : Kalitte.Trading.Algos.MaProfit
    {

    }

    public class backtest : Kalitte.Trading.Algos.MaProfit
    {
        public override void OnInit()
        {
            this.BackTestMode = true;
            base.OnInit();
        }
    }

    public class log : Kalitte.Trading.Algos.PriceLogger
    {

    }
}
