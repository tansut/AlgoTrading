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
        public override ProfitOrLoss SignalType => ProfitOrLoss.Loss;       

        public LossSignal(string name, string symbol, AlgoBase owner, decimal priceChange, decimal initialQuantity, decimal quantityStep, decimal stepMultiplier, decimal priceStep, decimal keepQuantity) : base(name, symbol, owner, priceChange, initialQuantity, quantityStep, stepMultiplier, priceStep, keepQuantity)
        {

        }
    }
}
