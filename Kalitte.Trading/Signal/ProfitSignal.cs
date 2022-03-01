// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    internal class ProfitSignal : ProfitLossSignal
    {
        protected override ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        {
            BuySell? bs = null;
            var pl = marketPrice - portfolio.AvgCost;            

            if (InitialQuantity > 0 && portfolio.Side == BuySell.Buy && pl >= this.UsedPriceChange)
            {
                bs = BuySell.Sell;
            }
            else if (InitialQuantity > 0 && portfolio.Side == BuySell.Sell && -pl >= this.UsedPriceChange)
            {
                bs = BuySell.Buy;
            }
            else return null;

            var result = new ProfitLossResult(this, SystemTime.Now);
            result.finalResult = bs;
            result.Quantity = quantity;
            result.MarketPrice = marketPrice;
            result.PL = pl;
            result.Direction = ProfitOrLoss.Profit;
            return result;
        }

        public ProfitSignal(string name, string symbol, AlgoBase owner, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier)
        {

        }


    }
}
