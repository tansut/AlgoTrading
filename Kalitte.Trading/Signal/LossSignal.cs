// algo
using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    internal class LossSignal : PLSignal
    {
        public LossSignal(string name, string symbol, AlgoBase owner, PLSignalConfig config) : base(name, symbol, owner, config)
        {
        }

        public override SignalUsage Usage { get => SignalUsage.StopLoss; protected set => base.Usage = value; }


    }
}
