// algo
using Skender.Stock.Indicators;
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
        decimal? CurrentValue { get; }
        List<decimal?> Results { get; }
        int Periods { get; set; }
        string Symbol { get;  set; }


    }

    public abstract class TradingIndicator<R>: ITradingIndicator where R: ResultBase
    {
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public Kalitte.Trading.Matrix.AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; set; }

        public FinanceBars InputBars { get; }
        public FinanceList<R> ResultList { get; set; } = null;

        public int Periods { get; set; }

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.Symbol}]";
        }


        protected abstract decimal? ToValue(R result);

        public decimal? CurrentValue { get
            {
                return ResultList.Count > 0 ? ToValue(ResultList.Last): null;
            }
        }

        public List<decimal?> Results
        {
            get
            {
                return ResultList.List.Select(p=> ToValue(p)).ToList();
            }
        }

        public TradingIndicator(FinanceBars bars, FinanceList<R> initialResults = null)
        {
            //this.Algo = Algo;
            InputBars = bars;
            ResultList = initialResults == null ? new FinanceList<R>(0, null): initialResults;
        }

        public TradingIndicator()
        {

        }

        public abstract decimal NextValue(decimal newVal);
        public abstract R NextResult(IQuote quote);
        //public abstract decimal NextValue(R result);
    }
}
