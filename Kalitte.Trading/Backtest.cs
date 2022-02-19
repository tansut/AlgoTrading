using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class Backtest
    {
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public AlgoBase Algo { get; set; }

        public Backtest(AlgoBase algo, DateTime start, DateTime end)
        {
            Algo = algo;
            StartTime = start;
            FinishTime = end;
        }

        public void Start()
        {

        }

    }
}
