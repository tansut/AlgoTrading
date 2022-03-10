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
using Kalitte.Trading.Algos;

namespace Kalitte.Trading
{
    public enum ProfitOrLoss
    {
        Loss,
        Profit
    }

    public class PriceMonitor
    {
        public Gradient Grad { get; set; }
        public ProfitLossSignal Owner { get; set; }
        public ProfitLossResult Result { get; set; }
        public AnalyseList List { get; set; }


        public bool WorkingFor(ProfitLossResult newResult)
        {
            return Result.Quantity == newResult.Quantity
                && Result.finalResult == newResult.finalResult
                && Result.KeepQuantity == newResult.KeepQuantity
                && Result.Direction == newResult.Direction;
        }

        public PriceMonitor(ProfitLossResult signalResult)
        {
            this.Result = signalResult;
            this.Owner = signalResult.Signal as ProfitLossSignal;
            var resistanceRatio = 0.0015M;
            var alpha = 0.0005M;
            var l1 = Result.finalResult == BuySell.Buy ? Result.MarketPrice + Result.MarketPrice * resistanceRatio : Result.MarketPrice - Result.MarketPrice * resistanceRatio;
            var l2 = Result.finalResult == BuySell.Buy ? Result.MarketPrice - Result.MarketPrice * .1M : Result.MarketPrice + Result.MarketPrice * .1M;
            this.Grad = new Gradient(l1, l2, Owner.Algo);
            this.Grad.Tolerance = resistanceRatio;
            this.Grad.Alpha = alpha;
            this.List = new AnalyseList(4, Average.Ema);
        }

        public ProfitLossResult Check(decimal mp)
        {            
            var price = List.Collect(mp).LastValue;
            var gradResult = this.Grad.Step(price);
            if (gradResult.FinalResult.HasValue)
            {
                return this.Result;
            }
            else return null;
            
        }
    }



    public class ProfitLossResult : SignalResult
    {
        public decimal PL { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal PortfolioCost { get; set; }
        public ProfitOrLoss Direction { get; set; }
        public decimal Quantity { get; set; }
        public decimal KeepQuantity { get; set; }
        
        public ProfitLossResult(Signal signal, DateTime t) : base(signal, t)
        {

        }

        public override string ToString()
        {
            return $"{base.ToString()} [MP: {MarketPrice} OP:{OriginalPrice} Q:{Quantity} PL:{PL}]";
        }

        public override int GetHashCode()
        {
            return this.SignalTime.GetHashCode();
        }
    }


    public abstract class ProfitLossSignal : Signal
    {
        public virtual decimal UsedPriceChange { get; set; }
        public virtual decimal QuantityStepMultiplier { get; set; }
        public virtual decimal QuantityStep { get; set; }
        public virtual decimal PriceStep { get; set; }
        public virtual decimal PriceChange { get; set; }
        public virtual decimal InitialQuantity { get; set; }
        public virtual decimal KeepQuantity { get; set; }
        public bool UsePriceMonitor { get; set; } = true;
        public PriceMonitor PriceMonitor { get; protected set; }

        public Fibonacci FibonacciLevels { get; set; } = null;

        public abstract ProfitOrLoss SignalType { get;  }

        public List<Type> LimitingSignals { get; set; } = new List<Type>();


        public ProfitLossSignal(string name, string symbol, AlgoBase owner,
            decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep, decimal keepQuantity) : base(name, symbol, owner)
        {
            PriceChange = priceChange;
            InitialQuantity = initialQuantity;
            QuantityStep = quantityStep;
            QuantityStepMultiplier = stepMultiplier;
            UsedPriceChange = priceChange;
            PriceStep = priceStep;
            KeepQuantity = keepQuantity;
        }

        public override void ResetOrders()
        {
            UsedPriceChange = PriceChange;
            if (PriceMonitor != null)
            {
                Log($"Destroying price monitor: {PriceMonitor.Result}", LogLevel.Warning);  
                PriceMonitor = null;
            }
            
            base.ResetOrders();
        }

        public void IncrementParams()
        {
            UsedPriceChange += PriceStep;
        }

        protected virtual PriceMonitor CreatePriceMonitor(ProfitLossResult result)
        {
            var monitor = new PriceMonitor(result);            
            Log($"Created price monitor for {result.Direction}/{result.finalResult} to get a better price than {result.MarketPrice}", LogLevel.Warning);
            return monitor;
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {InitialQuantity}/{PriceChange}";
        }

        protected override void ResetInternal()
        {
            ResetOrders();
        }

        public virtual decimal GetQuantity()
        {
            return this.CompletedOrder == 0 ? InitialQuantity : this.QuantityStep + (this.CompletedOrder) * QuantityStepMultiplier;
        }


        protected virtual ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        {
            BuySell? bs = null;
            var unitPl = marketPrice - portfolio.AvgCost;
            var totalPl = unitPl * portfolio.Quantity;

            if (this.LimitingSignals.Any())
            {
                var valid = portfolio.CompletedOrders.Count == 0 || portfolio.IsLastOrderInstanceOf(this.LimitingSignals.ToArray());
                if (!valid) return null;
            }

            if (this.SignalType == ProfitOrLoss.Profit)
            {
                if (InitialQuantity > 0 && portfolio.IsLong && unitPl >= this.UsedPriceChange)
                {
                    bs = BuySell.Sell;
                }
                else if (InitialQuantity > 0 && portfolio.IsShort && -unitPl >= this.UsedPriceChange)
                {
                    bs = BuySell.Buy;
                }
            }
            else if (this.SignalType == ProfitOrLoss.Loss)
            {               
                if (InitialQuantity > 0 && portfolio.IsLong && totalPl <= -this.UsedPriceChange)
                {
                    bs = BuySell.Sell;
                }
                else if (InitialQuantity > 0 && portfolio.IsShort && totalPl >= this.UsedPriceChange)
                {
                    bs = BuySell.Buy;
                }
            }
            else return null;

            var result = new ProfitLossResult(this, Algo.Now);
            result.finalResult = bs;
            result.Quantity = quantity;
            result.MarketPrice = marketPrice;
            result.OriginalPrice = marketPrice;
            result.PL = unitPl;
            result.Direction = SignalType;
            result.KeepQuantity = this.KeepQuantity;

            if (UsePriceMonitor && result.finalResult.HasValue && result.Direction == ProfitOrLoss.Profit)
            {
                PriceMonitor = PriceMonitor == null ? CreatePriceMonitor(result) : PriceMonitor;
                if (!PriceMonitor.WorkingFor(result))
                {
                    Log($"Creating new PriceMonitor since there is a new result. {PriceMonitor.Result} vs {result}", LogLevel.Debug);
                    PriceMonitor = CreatePriceMonitor(result);
                }
                var monitorResult = PriceMonitor.Check(marketPrice);
                if (monitorResult != null && monitorResult.finalResult.HasValue)
                {
                    //Log($"price monitor resulted: [{monitorResult.finalResult} {monitorResult.Direction}]: original: {monitorResult.MarketPrice} current: {marketPrice}", LogLevel.Warning);
                    monitorResult.MarketPrice = marketPrice;
                    monitorResult.PL = marketPrice - portfolio.AvgCost;
                    PriceMonitor = null;
                }
                return monitorResult;
            }

            return result;
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);

            if (!portfolio.IsEmpty)
            {
                var price = Algo.GetMarketPrice(Symbol, t);
                if (price == 0)
                {
                    return null;
                }
                else
                {
                    var quantity = this.CompletedOrder == 0 ? InitialQuantity : this.QuantityStep + (this.CompletedOrder) * QuantityStepMultiplier;
                    return this.getResult(portfolio, price, quantity);
                }
            }
            else return null;
        }

    }
}
