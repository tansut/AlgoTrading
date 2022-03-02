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

        public override ProfitOrLoss SignalType => ProfitOrLoss.Profit;

        //protected override ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        //{
    

        //    if (InitialQuantity > 0 && portfolio.IsLong && pl >= this.UsedPriceChange)
        //    {
        //        bs = BuySell.Sell;
        //    }
        //    else if (InitialQuantity > 0 && portfolio.IsShort && -pl >= this.UsedPriceChange)
        //    {
        //        bs = BuySell.Buy;
        //    }
        //    else return null;

        //    var result = new ProfitLossResult(this, Algo.Now);
        //    result.finalResult = bs;
        //    result.Quantity = quantity;
        //    result.MarketPrice = marketPrice;
        //    result.PL = pl;
        //    result.Direction = ProfitOrLoss.Profit;
        //    result.KeepQuantity = this.KeepQuantity;
        //    return result;
        //}

        public ProfitSignal(string name, string symbol, AlgoBase owner, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep, decimal keepQuantity) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier,  priceStep, keepQuantity)
        {

        }


    }
}
