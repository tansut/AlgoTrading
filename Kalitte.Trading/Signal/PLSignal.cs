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


    public class PriceMonitor
    {
        public Gradient Grad { get; set; }
        public PLSignal Owner { get; set; }
        public ProfitLossResult Result { get; set; }
        public AnalyseList List { get; set; }


        public bool WorkingFor(ProfitLossResult newResult)
        {
            return Result.Quantity == newResult.Quantity
                && Result.finalResult == newResult.finalResult
                && Result.KeepQuantity == newResult.KeepQuantity;                
        }

        public PriceMonitor(ProfitLossResult signalResult)
        {
            this.Result = signalResult;
            this.Owner = signalResult.Signal as PLSignal;
            var resistanceRatio = Owner.Config.PriceMonitorTolerance;
            var alpha = Owner.Config.PriceMonitorLearnRate;
            var l1 = Result.finalResult == BuySell.Buy ? Result.MarketPrice + Result.MarketPrice * resistanceRatio : Result.MarketPrice - Result.MarketPrice * resistanceRatio;
            var l2 = Result.finalResult == BuySell.Buy ? Result.MarketPrice - Result.MarketPrice * .1M : Result.MarketPrice + Result.MarketPrice * .1M;
            this.Grad = new Gradient(l1, l2, Owner.Algo);
            this.Grad.Tolerance = resistanceRatio;
            this.Grad.LearnRate = alpha;
            this.Grad.FileName = Path.Combine(Owner.Algo.LogDir, this.Owner.Name + ".png");
            this.List = new AnalyseList(4, Average.Ema);
        }

        public void Reset()
        {
            this.Grad.Reset();
            this.List.Clear();
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
        public decimal Quantity { get; set; }
        public decimal KeepQuantity { get; set; }
        
        
        public ProfitLossResult(SignalBase signal, DateTime t) : base(signal, t)
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


    public class PLSignalConfig: SignalConfig
    {
        [AlgoParam(0)]
        public decimal InitialQuantity { get; set; }

        [AlgoParam(0)]
        public decimal KeepQuantity { get; set; }

        [AlgoParam(0)]
        public virtual decimal QuantityStepMultiplier { get; set; }

        [AlgoParam(1)]
        public virtual decimal QuantityStep { get; set; }

        [AlgoParam(0)]
        public virtual decimal Step { get; set; }

        [AlgoParam(0)]
        public virtual decimal StartAt { get; set; }

        [AlgoParam(false)]
        public bool PriceMonitor { get; set; }

        [AlgoParam(true)]
        public bool EnableLimitingSignalsOnStart { get; set; }

        [AlgoParam(0)]
        public virtual decimal PriceMonitorTolerance { get; set; }

        [AlgoParam(0)]
        public virtual decimal PriceMonitorLearnRate { get; set; }

        
    }


    public abstract class PLSignal : Signal<PLSignalConfig>
    {
        public virtual decimal UsedPriceChange { get; set; }
        public PriceMonitor PriceMonitor { get; protected set; }
        public Fibonacci FibonacciLevels { get; set; } = null;
        public List<Type> LimitingSignalTypes { get; set; } = new List<Type>();        
        public List<SignalBase> LimitingSignals { get; set; } = new List<SignalBase>();        
        public List<SignalBase> CostSignals { get; set; } = new List<SignalBase>();





        public PLSignal(string name, string symbol, AlgoBase owner, PLSignalConfig config) : base(name, symbol, owner, config)
        {
            UsedPriceChange = config.StartAt;
        }

        public override void ResetOrdersInternal()
        {
            UsedPriceChange = Config.StartAt;
            if (PriceMonitor != null)
            {
                Log($"Destroying price monitor: {PriceMonitor.Result}", LogLevel.Verbose);  
                PriceMonitor = null;
            }
            
            base.ResetOrdersInternal();
        }

        public void IncrementParams()
        {
            Monitor.Enter(OperationLock);
            try
            {
                UsedPriceChange += Config.Step;
            }
            finally
            {
                Monitor.Exit  (OperationLock);
            }            
        }

        protected virtual PriceMonitor CreatePriceMonitor(ProfitLossResult result)
        {
            var monitor = new PriceMonitor(result);            
            Log($"Created price monitor for {result.finalResult} to get a better price than {result.MarketPrice}", LogLevel.Verbose);
            return monitor;
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {Config.InitialQuantity}/{Config.StartAt}";
        }


        public virtual decimal GetQuantity()
        {
            return this.CompletedOrder == 0 ? Config.InitialQuantity : Config.QuantityStep + (this.CompletedOrder-1) * Config.QuantityStepMultiplier;
        }


        protected virtual ProfitLossResult getResult(PortfolioItem portfolio, AverageCostResult costStatus,  decimal marketPrice, decimal quantity)
        {
           
            BuySell? bs = null;
            var unitPl = marketPrice - costStatus.AverageCost;
            var targetChange = (costStatus.AverageCost * (UsedPriceChange / 100M)).ToCurrency();


            if (this.Usage == SignalUsage.TakeProfit)
            {
                if (portfolio.IsLong && unitPl >= targetChange)
                {
                    bs = BuySell.Sell;
                }
                else if (portfolio.IsShort && -unitPl >= targetChange)
                {
                    bs = BuySell.Buy;
                }
            }
            else if (this.Usage == SignalUsage.StopLoss)
            {               
                if (portfolio.IsLong && unitPl <= -targetChange)
                {
                    bs = BuySell.Sell;
                }
                else if (portfolio.IsShort && unitPl >= targetChange)
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
            result.KeepQuantity = RoundQuantity(costStatus.TotalQuantity * (Config.KeepQuantity / 100M));

            if (Config.PriceMonitor && result.finalResult.HasValue && this.Usage == SignalUsage.TakeProfit)
            {
                PriceMonitor = PriceMonitor ?? CreatePriceMonitor(result);
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
                    PriceMonitor.Reset();
                }
                return monitorResult;
            }

            return result;
        }

        decimal RoundQuantity(decimal quantity)
        {
            var q = Math.Round(quantity);
            q = q < 1M ? 1 : q;
            return q;
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);

            if (!portfolio.IsEmpty)
            {
                if (this.LimitingSignalTypes.Any())
                {
                    var valid = (portfolio.CompletedOrders.Count == 0 && Config.EnableLimitingSignalsOnStart) || portfolio.IsLastPositionOrderInstanceOf(this.LimitingSignalTypes.ToArray());
                    if (!valid) return null;
                }

                if (this.LimitingSignals.Any())
                {
                    var valid = (portfolio.CompletedOrders.Count == 0 && Config.EnableLimitingSignalsOnStart) || portfolio.IsLastPositionOrderInstanceOf(this.LimitingSignals.ToArray());
                    if (!valid) return null;
                }

                var price = Algo.GetMarketPrice(Symbol, t);
                if (price == 0)
                {
                    return null;
                }
                else
                {
                    var portfolioStatus = portfolio.LastAverageCost(CostSignals.ToArray());
                    var quantityRatio = this.CompletedOrder == 0 ? Config.InitialQuantity : Config.QuantityStep + Config.QuantityStep * (this.CompletedOrder-1) * Config.QuantityStepMultiplier;
                    var quantity = RoundQuantity(portfolioStatus.TotalQuantity * quantityRatio / 100M);
                    return quantity == 0 || portfolioStatus.AverageCost == 0 ? null: this.getResult(portfolio, portfolioStatus, price, quantity);
                }
            }
            else return null;
        }

    }
}
