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

        public decimal CommissionPaid { get; set; } = 0M;

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
                return (AvgCost * Quantity);
            }
        }

        public override string ToString()
        {
            return $"{this.Symbol}:{SideStr}/{Quantity}/Cost: {AvgCost} Total: {Total} PL: {PL} Commission: {CommissionPaid} NetPL: {PL - CommissionPaid}";
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
            this.CommissionPaid += position.CommissionPaid;
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
                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
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
                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
                    var profit = (delta * direction * (position.FilledUnitPrice - this.AvgCost));
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
                return this.Sum(p => p.Value.CommissionPaid);
            }
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
