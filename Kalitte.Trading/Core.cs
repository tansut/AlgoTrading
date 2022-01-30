//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Matriks.Data.Symbol;

//using Matriks.Engines;
//using Matriks.Indicators;
//using Matriks.Symbols;
//using Matriks.AlgoTrader;
//using Matriks.Trader.Core;
//using Matriks.Trader.Core.Fields;
//using Matriks.Trader.Core.TraderModels;
//using Matriks.Lean.Algotrader.AlgoBase;
//using Matriks.Lean.Algotrader.Models;
//using Matriks.Lean.Algotrader.Trading;
//using System.Timers;

//namespace Kalitte.Trading
//{
//    public class ExchangeOrder
//    {
//        public string Symbol;
//        public string Id;
//        public OrderSide Side;
//        public decimal UnitPrice;
//        public decimal Quantity;
//        public string Comment;
//        public decimal FilledUnitPrice
//        {
//            get; set;
//        }
//        public decimal FilledQuantity
//        {
//            get; set;
//        }


//        public decimal Total
//        {
//            get
//            {
//                return FilledUnitPrice * FilledQuantity;
//            }
//        }

//        public ExchangeOrder(string symbol, string id, OrderSide side, decimal quantity, decimal unitPrice, string comment = "")
//        {
//            this.Symbol = symbol;
//            this.Id = id;
//            this.Side = side;
//            this.Quantity = quantity;
//            this.UnitPrice = unitPrice;
//            this.Comment = comment;
//            this.FilledUnitPrice = 0;
//        }

//        public string SideStr
//        {
//            get
//            {
//                return this.Side == OrderSide.Buy ? "long" : "short";
//            }
//        }

//        public override string ToString()
//        {
//            return $"{this.Symbol}:{this.SideStr}/{this.Quantity}:{this.FilledQuantity}/{this.UnitPrice}:{this.FilledUnitPrice} {this.Comment}";
//        }

//        public ExchangeOrder Clone()
//        {
//            var clone = new ExchangeOrder(this.Symbol, "", this.Side, this.Quantity, this.UnitPrice);
//            clone.FilledUnitPrice = this.FilledUnitPrice;
//            clone.FilledQuantity = this.FilledQuantity;
//            return clone;
//        }
//    }

//    public class Portfolio
//    {
//        public string Symbol
//        {
//            get; private set;
//        }
//        public decimal PL
//        {
//            get; private set;
//        }
//        public decimal AvgCost
//        {
//            get; private set;
//        }
//        public decimal Quantity
//        {
//            get; private set;
//        }
//        public OrderSide Side
//        {
//            get; set;
//        }

//        public bool IsLong
//        {
//            get
//            {
//                return this.Quantity > 0 && this.Side == OrderSide.Buy;
//            }
//        }

//        public bool IsShort
//        {
//            get
//            {
//                return this.Quantity > 0 && this.Side == OrderSide.Sell;
//            }
//        }

//        public bool IsEmpty
//        {
//            get
//            {
//                return this.Quantity <= 0;
//            }
//        }

//        public string SideStr
//        {
//            get
//            {
//                return this.Side == OrderSide.Buy ? "long" : "short";
//            }
//        }

//        public decimal Total
//        {
//            get
//            {
//                return AvgCost * Quantity;
//            }
//        }

//        public override string ToString()
//        {
//            return $"{this.Symbol}:{SideStr}/{Quantity}/Cost: {AvgCost} Total: {Total} PL: {PL}";
//        }

//        public Portfolio(string symbol, OrderSide side, decimal quantity, decimal unitPrice)
//        {
//            this.Symbol = symbol;
//            this.Side = side;
//            this.Quantity = quantity;
//            this.AvgCost = unitPrice;
//        }
//        public Portfolio(string symbol) : this(symbol, OrderSide.Buy, 0, 0)
//        {

//        }

//        public void OrderCompleted(ExchangeOrder position)
//        {
//            if (this.IsEmpty)
//            {
//                this.Side = position.Side;
//                this.Quantity = position.FilledQuantity;
//                this.AvgCost = position.FilledUnitPrice;
//            }
//            else
//            if (this.Side == position.Side)
//            {
//                this.AvgCost = (this.Total + position.Total) / (this.Quantity + position.FilledQuantity);
//                this.Quantity += position.FilledQuantity;

//            }
//            else
//            {
//                if (this.Quantity == position.FilledQuantity)
//                {
//                    this.AvgCost = 0;
//                    this.Quantity = 0;
//                }
//                else if (this.Quantity > position.FilledQuantity)
//                {
//                    var delta = position.FilledQuantity;
//                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
//                    var profit = delta * direction * (position.FilledUnitPrice - this.AvgCost);
//                    PL += profit;
//                    this.Quantity -= position.FilledQuantity;
//                    if (this.Quantity == 0)
//                    {
//                        this.AvgCost = 0;
//                    }
//                }
//                else
//                {
//                    var delta = this.Quantity;
//                    var direction = this.Side == OrderSide.Buy ? 1 : -1;
//                    var profit = delta * direction * (position.FilledUnitPrice - this.AvgCost);
//                    PL += profit;
//                    this.Side = position.Side;
//                    this.Quantity = position.FilledQuantity - this.Quantity;
//                    this.AvgCost = position.FilledUnitPrice;
//                }
//            }
//        }
//    }
//    public class PortfolioList
//    {
//        private Portfolio portfolio = null;

//        public Portfolio GetPortfolio(string symbol)
//        {
//            if (portfolio == null) portfolio = new Portfolio(symbol);
//            return portfolio;
//        }

//        public PortfolioList()
//        {

//        }

//        public Portfolio Portfolio
//        {
//            get
//            {
//                return this.portfolio;
//            }
//        }


//        public Portfolio Add(ExchangeOrder position)
//        {
//            var portfolio = this.GetPortfolio(position.Symbol);
//            //if (portfolio == null)
//            //{
//            //    this.portfolio = new Portfolio(position.Symbol, position.Side, position.FilledQuantity, position.FilledUnitPrice);
//            //}
//            portfolio.OrderCompleted(position);
//            return this.Portfolio;
//        }
//    }


//}
