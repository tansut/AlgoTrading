// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    internal class LossSignal : ProfitLossSignal
    {
        protected override ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        {
            BuySell? bs = null;
            var pl = marketPrice - portfolio.AvgCost;
            if (InitialQuantity > 0 && portfolio.IsLong && pl <= -this.UsedPriceChange)
            {

                bs = BuySell.Sell;

            }
            else if (InitialQuantity > 0 && portfolio.IsShort && pl >= this.UsedPriceChange)
            {
                bs = BuySell.Buy;

            }
            else return null;

            var result = new ProfitLossResult(this, Algo.Now);
            result.finalResult = bs;
            result.Quantity = quantity;
            result.MarketPrice = marketPrice;
            result.PL = pl;
            result.Direction = ProfitOrLoss.Loss;
            return result;
        }

        public LossSignal(string name, string symbol, AlgoBase owner, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier, priceStep)
        {

        }
    }
}
