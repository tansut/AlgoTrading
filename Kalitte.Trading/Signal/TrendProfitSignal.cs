// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    internal class TrendProfitSignal : ProfitLossSignal
    {
        public TrendSignal TrendSignal { get; set; }
        public decimal TrendThreshold { get; private set; }

        protected override ProfitLossResult getResult(PortfolioItem portfolio, decimal marketPrice, decimal quantity)
        {
            return null;
        }



        public ProfitLossResult HandleTrendSignal(TrendSignal signal, TrendSignalResult signalResult)
        {
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);

            if (!portfolio.IsEmpty)
            {
                var price = Algo.GetMarketPrice(Symbol, Algo.Now);
                if (price == 0)
                {
                    return null;
                }
                else
                {
                    BuySell? bs = null;
                    var pl = price - portfolio.AvgCost;
                    var quantity = GetQuantity();
                    var trend = signalResult.Trend;
                    var speed = trend.SpeedPerSecond;
                    if (Math.Abs(trend.Change) < TrendThreshold) return null;
                    else if ((trend.Direction == TrendDirection.ReturnDown || trend.Direction == TrendDirection.MoreUp) && InitialQuantity > 0 && portfolio.IsLong && pl >= this.UsedPriceChange)
                    {
                        bs = BuySell.Sell;
                    }
                    else if ((trend.Direction == TrendDirection.ReturnUp || trend.Direction == TrendDirection.LessDown) && InitialQuantity > 0 && portfolio.IsShort && -pl >= this.UsedPriceChange)
                    {
                        bs = BuySell.Buy;
                    }
                    else return null;

                    var result = new ProfitLossResult(this, Algo.Now);
                    result.finalResult = bs;
                    result.Quantity = quantity;
                    result.MarketPrice = price;
                    result.PL = pl;
                    result.Direction = ProfitOrLoss.Profit;
                    return result;
                }
            }
            else return null;            



        }




        public TrendProfitSignal(string name, string symbol, AlgoBase owner, TrendSignal signal, decimal trendThreshold, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier, priceStep)
        {
            this.TrendSignal = signal;
            TrendThreshold = trendThreshold;
        }


    }
}
