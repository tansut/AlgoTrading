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
    public class PortfolioItem
    {



        public decimal Commission { get; set; } = 0M;

        public string Symbol
        {
            get;  set;
        }
        public decimal PL
        {
            get;  set;
        }

        public decimal NetPL
        {
            get
            {
                return PL - Commission;
            }
        }
        public decimal AvgCost
        {
            get;  set;
        }
        public decimal Quantity
        {
            get;  set;
        }
        public BuySell Side
        {
            get; set;
        }

        public bool IsLong
        {
            get
            {
                return this.Quantity > 0 && this.Side == BuySell.Buy;
            }
        }

        public bool IsShort
        {
            get
            {
                return this.Quantity > 0 && this.Side == BuySell.Sell;
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
                return this.Side == BuySell.Buy ? "long" : "short";
            }
        }

        public decimal Total
        {
            get
            {
                return (AvgCost * Quantity);
            }
        }

        public override string ToString()
        {
            return $"{this.Symbol}:{SideStr}/{Quantity}/Cost: {AvgCost} Total: {Total} PL: {PL} Commission: {Commission} NetPL: {PL - Commission}";
        }

        public PortfolioItem(string symbol, BuySell side, decimal quantity, decimal unitPrice)
        {
            this.Symbol = symbol;
            this.Side = side;
            this.Quantity = quantity;
            this.AvgCost = unitPrice;
        }

        public PortfolioItem(string symbol) : this(symbol, BuySell.Buy, 0, 0)
        {

        }

        public List<ExchangeOrder> RequestedOrders { get; private set; } = new List<ExchangeOrder>();
        public List<ExchangeOrder> CompletedOrders { get; private set; } = new List<ExchangeOrder>();
        
        public void AddRequestedOrder(ExchangeOrder o)
        {
            this.RequestedOrders.Add(o);
        }

        public void OrderCompleted(ExchangeOrder position)
        {
            this.Commission += position.CommissionPaid;
            if (this.IsEmpty)
            {
                this.Side = position.Side;
                this.Quantity = position.FilledQuantity;
                this.AvgCost = position.FilledUnitPrice;
            }
            else if (this.Side == position.Side)
            {
                this.AvgCost = ((this.Total + position.Total) / (this.Quantity + position.FilledQuantity));
                this.Quantity += position.FilledQuantity;

            }
            else
            {
                if (this.Quantity >= position.FilledQuantity)
                {
                    var delta = position.FilledQuantity;
                    var direction = this.Side == BuySell.Buy ? 1 : -1;
                    var profit = (delta * direction * (position.FilledUnitPrice - this.AvgCost));
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
                    var direction = this.Side == BuySell.Buy ? 1 : -1;
                    var profit = (delta * direction * (position.FilledUnitPrice - this.AvgCost));
                    PL += profit;
                    this.Side = position.Side;
                    this.Quantity = position.FilledQuantity - this.Quantity;
                    this.AvgCost = position.FilledUnitPrice;
                }
            }
            this.CompletedOrders.Add(position);
        }

        
        public bool IsLastOrderInstanceOf(params Type [] signalTypes)
        {
            var last = this.LastPositionOrder;
            if (last == null) return false;
            foreach (var type in signalTypes)
            {
                if (last.SignalResult.Signal.GetType().IsAssignableFrom(type)) return true;
            }
            return false;  
        }


        public ExchangeOrder LastPositionOrder
        {
            get
            {
                return this.CompletedOrders.LastOrDefault(p => (!(p.SignalResult.Signal is ProfitLossSignal)));
            }
        }
    }
    public class PortfolioList : Dictionary<string, PortfolioItem>
    {

        public PortfolioItem GetPortfolio(string symbol)
        {
            lock(this)
            {
                if (!this.ContainsKey(symbol)) this.Add(symbol, new PortfolioItem(symbol));
                return this[symbol];
            }
        }

        public PortfolioList()
        {

        }





        public decimal PL
        {
            get
            {
                return this.Sum(p => p.Value.PL);
            }
        }


        public decimal Comission
        {
            get
            {
                return this.Sum(p => p.Value.Commission);
            }
        }





        public PortfolioItem Add(ExchangeOrder position)
        {
            var portfolio = this.GetPortfolio(position.Symbol);
            portfolio.OrderCompleted(position);
            //this.CompletedOrders.Add(position);
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
    }

}
