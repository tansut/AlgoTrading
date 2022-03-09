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
     
        public ProfitSignal(string name, string symbol, AlgoBase owner, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep, decimal keepQuantity) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier,  priceStep, keepQuantity)
        {

        }
    }
}
