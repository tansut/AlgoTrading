// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{

    public interface ITradingIndicator
    {
        FinanceBars InputBars { get; }
        decimal NextValue(decimal newVal);
    }

    public abstract class TradingIndicator<R>: ITradingIndicator where R: ResultBase
    {
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public Kalitte.Trading.Algos.AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }

        public FinanceBars InputBars { get; }
        public FinanceList<R> Results { get; set; } = null;

        

        public TradingIndicator(FinanceBars bars, FinanceList<R> initialResults = null)
        {
            //this.Algo = Algo;
            InputBars = bars;
            Results = initialResults == null ? new FinanceList<R>(0, null): initialResults;
        }

        public abstract decimal NextValue(decimal newVal);
    }
}
