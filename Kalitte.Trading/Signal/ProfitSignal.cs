// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class ProfitSignal : PLSignal
    {
        public ProfitSignal(string name, string symbol, AlgoBase owner, PLSignalConfig config) : base(name, symbol, owner, config)
        {

        }

        public override SignalUsage Usage { get => SignalUsage.TakeProfit; protected set => base.Usage = value; }
    }
}
