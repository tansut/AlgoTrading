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
        public decimal UsedPriceChange { get; set; }
        public decimal QuantityStepMultiplier { get; set; }

        public decimal QuantityStep { get; set; }
        public decimal PriceChange { get; set; }
        public decimal InitialQuantity { get; set; }

        public int CompletedOrder = 0;
        public decimal CompletedQuantity = 0;

        public ProfitLossSignal(string name, string symbol, AlgoBase owner,
            decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier) : base(name, symbol, owner)
        {
            PriceChange = priceChange;
            InitialQuantity = initialQuantity;
            QuantityStep = quantityStep;
            QuantityStepMultiplier = stepMultiplier;
            UsedPriceChange = priceChange;
            CompletedOrder = 0;
            CompletedQuantity = 0;
        }

        public void ResetChanges()
        {
            UsedPriceChange = PriceChange;
            CompletedOrder = 0;
            CompletedQuantity = 0;
        }

        public void AdjustChanges(decimal quantity, decimal price, ProfitOrLoss p)
        {
            AdjustPriceChange(price, p);
            UsedPriceChange += (p == ProfitOrLoss.Profit ? price : 0);
        }

        public void AdjustPriceChange(decimal increment, ProfitOrLoss p)
        {
            UsedPriceChange += (p == ProfitOrLoss.Profit ? increment : 0);
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


        protected abstract ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice);

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
                    return this.getResult(portfolio, price);
                }
            }
            else return null;
        }

    }
}
