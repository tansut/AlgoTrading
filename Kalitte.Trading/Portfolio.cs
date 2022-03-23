// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{

    public class AverageCostResult
    {
        public List<ExchangeOrder> Orders { get; set; }
        public decimal TotalQuantity
        {
            get
            {
                return Orders.Sum(p => (p.FilledQuantity - p.DirectionChangedQuantity));
            }
        }

        public decimal AverageQuantity
        {
            get
            {
                if (Orders.Count == 0) return 0;
                return TotalQuantity / Orders.Count;
            }
        }

        public decimal Total
        {
            get
            {
                return Orders.Sum(p => (p.FilledUnitPrice * (p.FilledQuantity - p.DirectionChangedQuantity)));
            }
        }
        public decimal AverageCost
        {
            get
            {
                return TotalQuantity == 0 ? 0 : (Total / TotalQuantity).ToCurrency();
            }
        }
    }

    public class Statistics
    {
        public decimal NetPl { get; set; }
        public decimal Total { get; set; }
    }

    public class PortfolioItem
    {

        public SortedDictionary<DateTime, Statistics> DailyStats { get; set; } = new SortedDictionary<DateTime, Statistics>();

        public decimal Commission { get; set; } = 0M;

        public string Symbol
        {
            get; set;
        }
        public decimal PL
        {
            get; set;
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
            get; set;
        }
        public decimal Quantity
        {
            get; set;
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

        private void SetDailyStats(decimal pl, ExchangeOrder order)
        {            
            DailyStats.TryGetValue(order.Sent.Date, out Statistics stats);
            if (stats == null) stats = new Statistics();
            stats.NetPl = stats.NetPl + pl - order.CommissionPaid;
            stats.Total++;
            DailyStats[order.Sent.Date] = stats;
        }

        public Statistics GetDailyStats(DateTime time)
        {
            DailyStats.TryGetValue(time.Date, out Statistics stats);
            if (stats == null) stats = new Statistics();
            return stats;
        }

        public void OrderCompleted(ExchangeOrder position)
        {
            this.Commission += position.CommissionPaid;
            var profit = 0M;
            if (this.IsEmpty)
            {
                this.Side = position.Side;
                this.Quantity = position.FilledQuantity;
                this.AvgCost = position.FilledUnitPrice;
            }
            else if (this.Side == position.Side)
            {
                this.AvgCost = (((this.Total + position.Total) / (this.Quantity + position.FilledQuantity))).ToCurrency();
                this.Quantity += position.FilledQuantity;
            }
            else
            {
                if (this.Quantity >= position.FilledQuantity)
                {
                    var delta = position.FilledQuantity;
                    var direction = this.Side == BuySell.Buy ? 1 : -1;
                    profit = (delta * direction * (position.FilledUnitPrice - this.AvgCost));
                    PL += profit;
                    this.Quantity -= position.FilledQuantity;
                    if (this.Quantity == 0)
                    {
                        this.AvgCost = 0;
                    } else
                    {
                        // ? new avgcost?
                    }
                }
                else
                {
                    var delta = this.Quantity;
                    var direction = this.Side == BuySell.Buy ? 1 : -1;
                    profit = (delta * direction * (position.FilledUnitPrice - this.AvgCost));
                    PL += profit;
                    this.Side = position.Side;
                    this.Quantity = position.FilledQuantity - this.Quantity;
                    this.AvgCost = position.FilledUnitPrice;
                    position.DirectionChangedQuantity = delta;
                }
            }
            SetDailyStats(profit, position);
            this.CompletedOrders.Add(position);
        }



        public bool IsLastPositionOrderInstanceOf(params SignalBase[] signals)
        {
            var last = this.LastPositionOrder;
            if (last == null) return false;
            foreach (var signal in signals)
            {
                if (signal == last.SignalResult.Signal) return true;
            }
            return false;
        }

        public bool LastOrderIsLoss
        {
            get
            {
                var lastOrder = CompletedOrders.LastOrDefault();
                return lastOrder != null && lastOrder.Usage == OrderUsage.StopLoss;
            }
        }

        public bool LastOrderIsProfit
        {
            get
            {
                var lastOrder = CompletedOrders.LastOrDefault();
                return lastOrder != null && lastOrder.Usage == OrderUsage.TakeProfit;
            }
        }

        public ExchangeOrder GetLastPositionOrder(params Type[] signalTypes)
        {
            var last = this.LastPositionOrder;
            foreach (var type in signalTypes)
            {
                if (type.IsAssignableFrom(last.SignalResult.Signal.GetType())) return last;
            }
            return null;
        }

        public ExchangeOrder GetLastPositionOrder(params SignalBase[] signals)
        {
            var last = this.LastPositionOrder;
            if (last == null) return null;
            foreach (var signal in signals)
            {
                if (last.SignalResult.Signal == signal) return last;
            }
            return null;
        }

        public AverageCostResult LastAverageCost(params SignalBase[] signals)
        {
            var res = new AverageCostResult();
            res.Orders = GetLastPositionOrders(signals);
            return res;
        }

        public List<ExchangeOrder> GetLastPositionOrders( Type[] types)
        {
            var result = new List<ExchangeOrder>();            
            for (var i = CompletedOrders.Count - 1; i >= 0; i--)
            {
                var type = CompletedOrders[i].SignalResult.Signal.GetType();
                if (CompletedOrders[i].Usage != OrderUsage.CreatePosition)
                {
                    if (result.Count > 0) break;
                    continue;
                }
                if (types.Length == 0) result.Add(CompletedOrders[i]);
                else if (types.Any(t => t.IsAssignableFrom(type))) result.Add(CompletedOrders[i]);
                else break;
            }
            return result;
        }

        public List<ExchangeOrder> GetLastPositionOrders(params SignalBase[] signals)
        {
            var result = new List<ExchangeOrder>();
            
            for (var i = CompletedOrders.Count - 1; i >= 0; i--)
            {
                var type = CompletedOrders[i].SignalResult.Signal.GetType();
                if (CompletedOrders[i].Usage != OrderUsage.CreatePosition)
                {
                    if (result.Count > 0) break;
                    continue;
                }
                if (signals.Length == 0) result.Add(CompletedOrders[i]);
                else if (signals.Any(s => s == CompletedOrders[i].SignalResult.Signal)) result.Add(CompletedOrders[i]);                
                else break;
            }
            return result;
        }

        public bool IsLastPositionOrderInstanceOf(params Type[] signalTypes)
        {
            var last = this.LastPositionOrder;
            if (last == null) return false;
            foreach (var type in signalTypes)
            {
                if (type.IsAssignableFrom(last.SignalResult.Signal.GetType())) return true;
            }
            return false;
        }

        public bool IsLastPositionOrder(params SignalBase[] signals)
        {
            var last = GetLastPositionOrder(signals);
            return last != null;
        }

        public ExchangeOrder GetLastOrderSkip(Type skip)
        {
            for (var i = CompletedOrders.Count - 1; i >= 0; i--)
            {
                var type = CompletedOrders[i].SignalResult.Signal.GetType();
                if (skip.IsAssignableFrom(type)) continue;
                else return CompletedOrders[i];
            }
            return null;
        }



        public ExchangeOrder LastPositionOrder
        {
            get
            {
                for (var i = CompletedOrders.Count - 1; i >= 0; i--)
                {
                    var order = CompletedOrders[i];
                    if (order.Usage == OrderUsage.CreatePosition) return CompletedOrders[i];
                }
                return null;
            }
        }
    }
    public class PortfolioList : Dictionary<string, PortfolioItem>
    {

        public PortfolioItem GetPortfolio(string symbol)
        {
            lock (this)
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
