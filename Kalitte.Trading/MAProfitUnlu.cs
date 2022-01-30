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

namespace Kalitte.Trading
{
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
		public abstract void LogMarketPrice(DateTime t, decimal price);
		public abstract decimal GetMarketPrice(DateTime t);
	}

	public class MarketDataFileLogger : MarketDataLogger
	{
		public string Dir
		{
			get; set;
		}
		private string usedDir;
		private Dictionary<string, SortedList<string, decimal>> cache = new Dictionary<string, SortedList<string, decimal>>();
		public MarketDataFileLogger(string symbol, string dir) : base(symbol)
		{
			Dir = dir;
			usedDir = Path.Combine(Dir, Path.Combine(symbol, "price"));
			if (!Directory.Exists(usedDir)) Directory.CreateDirectory(usedDir);
		}

		public string GetFileName(DateTime t)
		{
			return Path.Combine(usedDir, t.ToString("yyyy-MM-dd"));
		}

		public override void LogMarketPrice(DateTime t, decimal price)
		{
			string append = $"{t.ToString("HH-mm-ss")}\t{price}\n";
			string file = GetFileName(t);
			File.AppendAllText(file, append);
		}

		public override decimal GetMarketPrice(DateTime t)
		{
			var file = GetFileName(t);
			var cacheContains = cache.ContainsKey(file);
			var content = cacheContains ? cache[file] : new SortedList<string, decimal>();
			if (!content.Any() && File.Exists(file))
			{
				var fileContent = File.ReadAllLines(file);
				foreach (var line in fileContent)
				{
					var parts = line.Split('\t');
					try
					{
						content.Add(parts[0], decimal.Parse(parts[1]));
					}
					catch (ArgumentException ex)
					{
						Console.WriteLine(ex.Message);
					}

				}
			}
			if (!cacheContains && !content.Any()) cache[file] = content;
			decimal result = 0;
			content.TryGetValue(t.ToString("HH-mm-ss"), out result);
			return result;
		}
	}
}



namespace Kalitte.Trading.Algos
{

	public class PriceLogger : MatriksAlgo
	{
		[Parameter(1)]
		public int LogSeconds = 1;

		[SymbolParameter("F_XU0300222")]
		public string Symbol = "F_XU0300222";


		public string logDir = @"c:\kalitte\log";
		private MarketDataFileLogger fileLogger;

		public override void OnInit()
		{
			AddSymbolMarketData(Symbol);
			SetTimerInterval(1);
			this.fileLogger = new MarketDataFileLogger(Symbol, logDir);
		}

		public override void OnTimer()
		{
			var t = DateTime.Now;
			var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
			var t2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);
			if (t >= t1 && t <= t2)
			{
				var price = GetMarketData(Symbol, SymbolUpdateField.Last);
				fileLogger.LogMarketPrice(DateTime.Now, price);
			}
		}
	}

	public class MaProfit : MatriksAlgo
	{
		//[SymbolParameter("F_XU0300222")]
		public string Symbol = "F_XU0300222";

		//[Parameter(SymbolPeriod.Min10)]
		public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

		[Parameter(2)]
		public decimal OrderQuantity = 2M;

		//[Parameter(5)]
		public int MovPeriod = 5;

		//[Parameter(9)]
		public int MovPeriod2 = 9;

		//[Parameter(true)]
		public bool DoublePositions = true;

		//[Parameter(true)]
		public bool EnableTakeProfit = true;

		//[Parameter(false)]
		public bool UseVirtualOrders = false;

		//[Parameter(false)]
		public bool AutoCompleteOrders = false;

		//[Parameter(false)]
		public bool SimulateOrderSignal = false;

		//[Parameter(9)]
		public decimal ProfitPuan = 9;

		//[Parameter(18)]
		//public decimal LossPuan = 18;


		//[Parameter(0)]
		public int RsiLong = 0;

		//[Parameter(0)]
		public int RsiShort = 0;

		//[Parameter(0)]
		public int MACDLongPeriod = 0;

		//[Parameter(5)]
		public int MACDShortPeriod = 5;

		//[Parameter(4)]
		public int MACDTrigger = 3;

		MACD macd;

		public string logDir = @"c:\kalitte\log";
		private MarketDataFileLogger fileLogger;


		//[Parameter(false)]
		public bool BackTestMode = false;

		int virtualOrderCounter = 0;
		MOV mov;
		MOV mov2;
		RSI rsi;
		decimal takeProfitTotal = 0;

		bool buy = true;


		PortfolioList portfolios = new PortfolioList();
		ExchangeOrder positionRequest = null;
		List<ExchangeOrder> orders = new List<ExchangeOrder>();

		System.Timers.Timer orderTimer;
		private object ordrLock = new object();

		public override void OnInit()
		{
			AddSymbol(Symbol, SymbolPeriod);
			mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
			mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
			rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
			macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);

			WorkWithPermanentSignal(true);
			SendOrderSequential(false);
			orderTimer = new System.Timers.Timer(3812);
			if (EnableTakeProfit && !BackTestMode)
			{
				AddSymbolMarketData(Symbol);
				SetTimerInterval(1);
			}
			this.fileLogger = new MarketDataFileLogger(Symbol, logDir);
		}

		public decimal GetMarketPrice(DateTime? t = null)
		{
			if (BackTestMode) return fileLogger.GetMarketPrice(t.HasValue ? t.Value : DateTime.Now);
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
			//		Debug("Portfolio Updated");
			//		Debug(portfolios.Print());
			//	}
			//}


			//Debug($"sym: {position.Symbol} side:{position.Side} total:{position.TotalPosition} amount:{position.Amount} cost:{position.AvgCost} avail:{position.QtyAvailable} net:{position.QtyNet}");

		}

		public void LoadRealPositions()
		{
			var positions = BackTestMode ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
			portfolios.LoadRealPositions(positions, p => p.Symbol == this.Symbol);
			Debug($"- PORTFOLIO -");
			Debug($"{portfolios.Print()}");
		}







		public override void OnInitCompleted()
		{
			var assembly = typeof(MaProfit).Assembly.GetName();
			Debug($"Inited with {assembly.FullName}");
			LoadRealPositions();
			orderTimer.Elapsed += OnOrderTimerEvent;
			orderTimer.AutoReset = true;
			orderTimer.Enabled = true;
		}

		private void OnOrderTimerEvent(Object source, ElapsedEventArgs e)
		{
			if (BackTestMode) return;
			lock (this.ordrLock)
			{
				this.CreateOrders(null);
			}
		}




		public bool ensureWaitingPositions()
		{
			if (this.positionRequest != null)
			{
				//Debug($"active position waiting: {positionRequest.Id}/{positionRequest.Symbol}/{positionRequest.Side}/{positionRequest.Quantity}");

				return false;
			}
			else return true;

		}

		public void TryTakeProfit(decimal? marketPrice = null)
		{
			if (!this.ensureWaitingPositions()) return;


			var portfolio = portfolios.GetPortfolio(Symbol);

			
			if (!portfolio.IsEmpty)
			{
				var price = marketPrice.HasValue ? marketPrice.Value : GetMarketPrice();
				var pl = price - portfolio.AvgCost;
				if (price == 0)
				{
					return;
				}

				
				if ((portfolio.Side == OrderSide.Buy) && (pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
				{
					takeProfitTotal += pl;
					sendOrder(Symbol, this.OrderQuantity / 2.0M, OrderSide.Sell, $"take profit order, PL: {pl}, totalTakeProfit: {takeProfitTotal}", price);

				}
				else if ((portfolio.Side == OrderSide.Sell) && (-pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
				{
					takeProfitTotal += (-pl);
					sendOrder(Symbol, this.OrderQuantity / 2.0M, OrderSide.Buy, $"take profit order, PL: {pl}, totalTakeProfit: {takeProfitTotal}", price);

				}
			}


		}

		public override void OnTimer()
		{
			lock (this.ordrLock)
			{
				TryTakeProfit();
			}
		}



		protected ExchangeOrder sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0)
		{
			if (!this.ensureWaitingPositions())
			{
				Debug("Bekleyen pozisyon varken yeni pozisyon gönderilemez");
				return null;
			}
			var price = lprice > 0 ? lprice : GetMarketPrice();
			string orderid;

			decimal limitPrice = Math.Round((price + price * 0.02M * (side == OrderSide.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;

			if (UseVirtualOrders)
			{
				orderid = virtualOrderCounter++.ToString();
			}
			else
			{
				//Debug($"Limit order: {limitPrice}");
				orderid = BackTestMode ? this.SendMarketOrder(symbol, quantity, side) :
				this.SendLimitOrder(symbol, quantity, side, limitPrice, ChartIcon.None, DateTime.Now.Hour >= 19);

			}
			//else orderid = DateTime.Now.Hour >= 19 ? this.SendMarketOrder(symbol, quantity, side, ChartIcon.None, true) :
			//this.SendMarketOrder(symbol, quantity, side);

			this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment);
			Debug($"Order created, waiting to complete: {this.positionRequest.ToString()}");
			this.orders.Add(positionRequest);
			if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
			return this.positionRequest;
		}

		public bool ensureSignal(Func<BarDataCurrentValues, bool> func, BarDataCurrentValues values, string caption = "")
        {
			var result = func(values);
			if (result)
            {
				var wait = 10;
				Debug($"Waiting to confirm {caption} signal in {wait} seconds...");
				Thread.Sleep(wait * 1000);
				result = func(values);
				if (!result)
                {
					Debug(@"{caption} signal not confirmed.");
                }
			}
			return result;
        }

		public bool buySignal(BarDataCurrentValues barDataCurrentValues)
		{

			if (SimulateOrderSignal) return buy;
			var maSignal = MovPeriod > 0 ? CrossAbove(mov, mov2) : true;
			var rsiSignal = RsiLong > 0 ? CrossAbove(rsi, RsiLong) : true;
			var macdSignal = MACDLongPeriod > 0 ? CrossAbove(macd, macd.MacdTrigger) : true;



			//Debug($"Long? rsi: {rsi.CurrentValue} ma: {mov.CurrentValue} ma2: {mov2.CurrentValue} maSignal: {maSignal} rsiSignal: {rsiSignal}");            
			//return  macdSignal;
			//Debug($"Status (Long): ma: {CrossAbove(mov, mov2)} rsi: {CrossAbove(rsi, RsiLong)} macd: {CrossAbove(macd, macd.MacdTrigger)}");
			return maSignal && rsiSignal && macdSignal;
		}


		public bool sellSignal(BarDataCurrentValues barDataCurrentValues)
		{
			if (SimulateOrderSignal) return !buy;

			var maSignal = MovPeriod > 0 ? CrossBelow(mov, mov2) : true;
			var rsiSignal = RsiShort > 0 ? CrossBelow(rsi, RsiShort) : true;
			var macdSignal = MACDLongPeriod > 0 ? CrossBelow(macd, macd.MacdTrigger) : true;

			//Debug($"Status (Short): ma: {CrossBelow(mov, mov2)} rsi: {CrossBelow(rsi, RsiLong)} macd: {CrossBelow(macd, macd.MacdTrigger)}");
			//Debug($"Short? rsi: {rsi.CurrentValue} ma: {mov.CurrentValue} ma2: {mov2.CurrentValue} maSignal: {maSignal} rsiSignal: {rsiSignal}");
			return maSignal && rsiSignal && macdSignal;

			//return macdSignalCrossBelow
			


			//var signal = CrossBelow(mov, mov2) && (RsiShort == 0 || (rsi.CurrentValue >= RsiShort));
			//Debug($"Short? rsi: {rsi.CurrentValue} ma: {mov.CurrentValue} ma2: {mov2.CurrentValue} signal: {signal}");
			//return signal;
		}

		public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
		{
			if (!this.BackTestMode) return;
			if (EnableTakeProfit)
            {
				var t = barDataCurrentValues.LastUpdate.DTime;
				var price = GetMarketPrice(t);
				if (price == 0)
				{
					Debug($"{t.ToString()} has no market price");
				}

				this.TryTakeProfit(price);
			}
			this.CreateOrders(barDataCurrentValues);
		}

		public void CreateOrders(BarDataCurrentValues barDataCurrentValues)
		{
			if (!this.ensureWaitingPositions()) return;
			decimal doubleMultiplier = 1.0M;
			OrderSide? side = null;
			var portfolio = this.portfolios.GetPortfolio(Symbol);


			if (ensureSignal(buySignal, barDataCurrentValues, "LONG"))
			{
				if (!portfolio.IsLong)
				{
					buy = false;
					side = OrderSide.Buy;
					if (this.DoublePositions)
					{
						if (portfolio.IsShort)
						{
							doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && EnableTakeProfit) ? 1.5M : 2.0M;
						}
					}
				}
				else
				{
					//Debug($"Al geldi Portfolio LONG olduğu için gönderilmedi");
					//Debug($"{portfolios.Print()}");
				}

			}

			else if (ensureSignal(sellSignal, barDataCurrentValues, "SELL"))
			{
				if (!portfolio.IsShort)
				{
					buy = true;
					side = OrderSide.Sell;
					if (this.DoublePositions)
					{
						if (portfolio.IsLong)
						{
							doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && EnableTakeProfit) ? 1.5M : 2.0M;
						}
					}
				}
				else
				{
					//Debug($"Sat geldi Portfolio SHRT olduğu için gönderilmedi");
					//Debug($"{portfolios.Print()}");
				}

			}
			if (side != null)
			{
				sendOrder(Symbol, OrderQuantity * doubleMultiplier, side.Value);
			}
			else
			{
				//Debug("İşlemlik durum oluşmadı");
			}
		}

		public void FillCurrentOrder(decimal filledUnitPrice, decimal filledQuantity)
		{
			this.positionRequest.FilledUnitPrice = filledUnitPrice;
			this.positionRequest.FilledQuantity = filledQuantity;
			var portfolio = this.portfolios.Add(this.positionRequest);
			Debug($"Completed order: {this.positionRequest.ToString()}");
			Debug($"Portfolio: {portfolio.ToString()}");

			this.positionRequest = null;
		}

		public override void OnOrderUpdate(IOrder order)
		{
			if (order.OrdStatus.Obj == OrdStatus.Filled)
			{
				//Debug($"fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}");
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
			Debug($"Portfolio ended: {portfolios.Print()}");
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
