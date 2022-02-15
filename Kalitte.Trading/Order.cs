﻿// algo
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

    public class ExchangeOrder
    {
        public DateTime Created { get; set; } 
        public DateTime Sent { get; set; }
        public DateTime Resulted { get; set; }
        public string Symbol;
        public string Id;
        public OrderSide Side;
        public decimal UnitPrice;
        public decimal Quantity;
        public string Comment;
        public decimal ComissionRate = 0.0002M;
        public decimal FilledUnitPrice
        {
            get; set;
        }

        public decimal FilledQuantity
        {
            get; set;
        }

        public decimal CommissionPaid
        {
            get
            {
                return (Total * ComissionRate).ToCurrency();
            }
        }


        public decimal Total
        {
            get
            {
                return (FilledUnitPrice * FilledQuantity);
            }
        }

        public SignalResultX OriginSignal { get; set; }

        public ExchangeOrder(string symbol, string id, OrderSide side, decimal quantity, decimal unitPrice, string comment = "", DateTime? t = null)
        {
            this.Symbol = symbol;
            this.Id = id;
            this.Side = side;
            this.Quantity = quantity;
            this.UnitPrice = unitPrice;
            this.Comment = comment;
            this.FilledUnitPrice = 0M;
            this.Created = t ?? DateTime.Now;
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
            return $"[{this.Id}]{this.Symbol}:{this.SideStr}/{this.Quantity}:{this.FilledQuantity}/{this.UnitPrice}:{this.FilledUnitPrice} {this.Comment} Commission: {CommissionPaid}";
        }

        public ExchangeOrder Clone()
        {
            var clone = new ExchangeOrder(this.Symbol, "", this.Side, this.Quantity, this.UnitPrice);
            clone.ComissionRate = this.ComissionRate;
            clone.FilledUnitPrice = this.FilledUnitPrice;
            clone.FilledQuantity = this.FilledQuantity;
            return clone;
        }
    }

}
