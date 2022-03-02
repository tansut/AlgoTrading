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

    public class ProfitLossResult : SignalResult
    {
        public decimal PL { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal PortfolioCost { get; set; }
        public ProfitOrLoss Direction { get; set; }
        public decimal Quantity { get; set; }
        public decimal KeepQuantity { get; set; }

        public ProfitLossResult(Signal signal, DateTime t) : base(signal, t)
        {

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

        public abstract ProfitOrLoss SignalType { get;  }

        public int CompletedOrder = 0;
        public decimal CompletedQuantity = 0;

        public ProfitLossSignal(string name, string symbol, AlgoBase owner,
            decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep, decimal keepQuantity) : base(name, symbol, owner)
        {
            PriceChange = priceChange;
            InitialQuantity = initialQuantity;
            QuantityStep = quantityStep;
            QuantityStepMultiplier = stepMultiplier;
            UsedPriceChange = priceChange;
            PriceStep = priceStep;
            CompletedOrder = 0;
            CompletedQuantity = 0;
            KeepQuantity = keepQuantity;
        }

        public void ResetChanges()
        {
            UsedPriceChange = PriceChange;
            CompletedOrder = 0;
            CompletedQuantity = 0;
        }

        public void IncrementParams()
        {
            UsedPriceChange += PriceStep;
        }




        public void IncrementSignal(int orderInc, decimal quantityInc)
        {
            CompletedOrder += orderInc;
            CompletedQuantity += quantityInc;
        }


        public override string ToString()
        {
            return $"{base.ToString()}: {InitialQuantity}/{PriceChange}";
        }

        protected override void ResetInternal()
        {
            ResetChanges();
        }

        public virtual decimal GetQuantity()
        {
            return this.CompletedOrder == 0 ? InitialQuantity : this.QuantityStep + (this.CompletedOrder) * QuantityStepMultiplier;
        }

        protected virtual ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        {
            BuySell? bs = null;
            var pl = marketPrice - portfolio.AvgCost;

            if (this.SignalType == ProfitOrLoss.Profit)
            {
                if (InitialQuantity > 0 && portfolio.IsLong && pl >= this.UsedPriceChange)
                {
                    bs = BuySell.Sell;
                }
                else if (InitialQuantity > 0 && portfolio.IsShort && -pl >= this.UsedPriceChange)
                {
                    bs = BuySell.Buy;
                }
            }
            else if (this.SignalType == ProfitOrLoss.Loss)
            {
                if (InitialQuantity > 0 && portfolio.IsLong && pl <= -this.UsedPriceChange)
                {

                    bs = BuySell.Sell;
                }
                else if (InitialQuantity > 0 && portfolio.IsShort && pl >= this.UsedPriceChange)
                {
                    bs = BuySell.Buy;
                }
            }
            else return null;

            var result = new ProfitLossResult(this, Algo.Now);
            result.finalResult = bs;
            result.Quantity = quantity;
            result.MarketPrice = marketPrice;
            result.PL = pl;
            result.Direction = SignalType;
            result.KeepQuantity = this.KeepQuantity;
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
