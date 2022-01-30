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
using Kalitte.Trading;
using System.Text;
using System.Collections.Concurrent;

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


}



namespace Kalitte.Trading.Algos
{

	public class MaProfit : MatriksAlgo
	{
		[SymbolParameter("F_XU0300222")]
		public string Symbol;

		[Parameter(SymbolPeriod.Min10)]
		public SymbolPeriod SymbolPeriod;

		[Parameter(2)]
		public decimal OrderQuantity;

		[Parameter(5)]
		public int MovPeriod;

		[Parameter(9)]
		public int MovPeriod2;

		[Parameter(true)]
		public bool DoublePositions;

		[Parameter(true)]
		public bool EnableTakeProfit;

		[Parameter(true)]
		public bool UseVirtualOrders;

		[Parameter(false)]
		public bool AutoCompleteOrders;

		[Parameter(false)]
		public bool SimulateOrderSignal;

		[Parameter(9)]
		public decimal ProfitPuan;

		int virtualOrderCounter = 0;
		MOV mov;
		MOV mov2;
		Dictionary<string, AlgoTraderPosition> positions = null;
		int positionCount = 0;
		int dataUpdateCount = 0;
		int longSignal = 0;
		int shortSignal = 0;
		bool insideDataUpdate = false;
		bool insideTimerUpdate = false;
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
			if (EnableTakeProfit) AddSymbolMarketData(Symbol);

			mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
			mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);

			WorkWithPermanentSignal(true);
			SendOrderSequential(false);
			SetTimerInterval(3);
			orderTimer = new System.Timers.Timer(10000);


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


			Debug($"sym: {position.Symbol} side:{position.Side} total:{position.TotalPosition} amount:{position.Amount} cost:{position.AvgCost} avail:{position.QtyAvailable} net:{position.QtyNet}");

		}

		public void LoadRealPositions()
		{
			//Debug($"load start: {DateTime.Now.ToLongTimeString()}");
			var positions = GetRealPositions();
			//Debug($"load end: {DateTime.Now.ToLongTimeString()}");
			//if (!PositionReceiveComplated)
			//{
			//    Debug($"not completed");
			//}
			portfolios.LoadRealPositions(positions, p => p.Symbol == this.Symbol);
			Debug($"- PORTFOLIO -");
			Debug($"{portfolios.Print()}");
		}



		public override void OnInitCompleted()
		{
			LoadRealPositions();
			orderTimer.Elapsed += OnOrderTimerEvent;
			orderTimer.AutoReset = true;
			orderTimer.Enabled = true;
		}

		private void OnOrderTimerEvent(Object source, ElapsedEventArgs e)
		{
			lock (this.ordrLock)
			{
				this.CreateOrders(null);
			}
		}




		public bool ensureWaitingPositions()
		{
			if (this.positionRequest != null)
			{
				Debug($"active position waiting: {positionRequest.Id}/{positionRequest.Symbol}/{positionRequest.Side}/{positionRequest.Quantity}");

				return false;
			}
			else return true;

		}

		public void CheckTakeProfit()
		{
			if (!this.EnableTakeProfit) return;
			if (!this.ensureWaitingPositions()) return;


			//LoadRealPositions();
			var portfolio = portfolios.GetPortfolio(Symbol);
			if (!portfolio.IsEmpty)
			{
				var price = GetMarketData(Symbol, SymbolUpdateField.Last);
				var pl = price - portfolio.AvgCost;
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
				CheckTakeProfit();
			}
		}



		protected ExchangeOrder sendOrder(string symbol, decimal quantity, OrderSide side, string comment = "", decimal lprice = 0)
		{
			if (!this.ensureWaitingPositions())
			{
				Debug("Bekleyen pozisyon varken yeni pozisyon gönderilemez");
				return null;
			}
			var price = lprice > 0 ? lprice : GetMarketData(symbol, SymbolUpdateField.Last);
			string orderid;
			if (UseVirtualOrders)
			{
				orderid = virtualOrderCounter++.ToString();
			}
			else orderid = DateTime.Now.Hour >= 19 ? this.SendMarketOrder(symbol, quantity, side, ChartIcon.None, true) :
			this.SendMarketOrder(symbol, quantity, side);
			this.positionRequest = new ExchangeOrder(symbol, orderid, side, quantity, price, comment);
			Debug($"New order: {this.positionRequest.ToString()}");
			this.orders.Add(positionRequest);
			if (this.UseVirtualOrders || this.AutoCompleteOrders) FillCurrentOrder(positionRequest.UnitPrice, positionRequest.Quantity);
			return this.positionRequest;
		}

		public bool buySignal(BarDataCurrentValues barDataCurrentValues)
		{
			return SimulateOrderSignal ? buy : CrossAbove(mov, mov2);
		}

		public bool sellSignal(BarDataCurrentValues barDataCurrentValues)
		{

			return SimulateOrderSignal ? !buy : CrossBelow(mov, mov2);
		}

		public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
		{
			//this.CreateOrders();
		}

		public void CreateOrders(BarDataCurrentValues barDataCurrentValues)
		{
			if (!this.ensureWaitingPositions()) return;
			this.dataUpdateCount++;
			decimal doubleMultiplier = 1.0M;
			OrderSide? side = null;
			var portfolio = this.portfolios.GetPortfolio(Symbol);
						

			if (buySignal(barDataCurrentValues))
			{
				if (!portfolio.IsLong)
                {
					buy = false;
					this.longSignal++;
					side = OrderSide.Buy;
					if (this.DoublePositions)
					{
						if (portfolio.IsShort)
						{
							doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && EnableTakeProfit) ? 1.5M : 2.0M;
						}
					}
				} else
                {
					//Debug($"Al geldi Portfolio LONG olduğu için gönderilmedi");
					//Debug($"{portfolios.Print()}");
                }

			}

			else if (sellSignal(barDataCurrentValues))
			{
				if (!portfolio.IsShort)
                {
					buy = true;
					this.shortSignal++;
					side = OrderSide.Sell;
					if (this.DoublePositions)
					{
						if (portfolio.IsLong)
						{
							doubleMultiplier = ((portfolio.Quantity == OrderQuantity / 2.0M) && EnableTakeProfit) ? 1.5M : 2.0M;
						}
					}
				} else
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
			Debug($"Order completed: {this.positionRequest.ToString()}");
			Debug($"Portfolio: {portfolio.ToString()}");

			this.positionRequest = null;
		}

		public override void OnOrderUpdate(IOrder order)
		{


			if (order.OrdStatus.Obj == OrdStatus.Filled)
			{

				if (this.positionRequest != null && this.positionRequest.Id == order.CliOrdID)
				{
					this.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
				}
			}
		}



		public override void OnStopped()
		{
			orderTimer.Stop();
		}

	}

}

namespace Matriks.Lean.Algotrader
{
	public class MaProfitAlg : Kalitte.Trading.Algos.MaProfit
	{

	}
}
