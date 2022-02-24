// algo
using Kalitte.Trading.Algos;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{

    public class IndicatorResult
    {
        public DateTime Date { get; set; }
        public decimal? Value { get; set; }

        public IndicatorResult(DateTime date, decimal? value)
        {
            Date = date;    
            Value = value;
        }
    }

    public interface ITechnicalIndicator
    {
        FinanceBars InputBars { get; }
        decimal NextValue(decimal newVal);
        decimal? CurrentValue { get; }
        List<IndicatorResult> Results { get; }
        int Lookback { get; set; }
        string Symbol { get;  set; }
        List<IQuote> UsedInput { get; }

        
        //CandlePart Candle { get; set; }

    }

    public abstract class IndicatorBase<R>: ITechnicalIndicator where R: ResultBase
    {
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; set; }
        public CandlePart Candle { get; set; } = CandlePart.Close;
        public FinanceBars InputBars { get; }
        public FinanceList<R> ResultList { get; set; } = null;

        private  List<IQuote> usedBars;

        public int Lookback { get; set; }

        public override string ToString()
        {
            return $"{this.GetType().Name}[{this.Symbol}]";
        }

        public List<IQuote> UsedInput { get
            {if (usedBars == null) usedBars = CreateUsedBars();
                return usedBars;
            } 
        }

        protected abstract IndicatorResult ToValue(R result);

        public decimal? CurrentValue { get
            {
                return ResultList.Count > 0 ? ToValue(ResultList.Last).Value: null;
            }
        }

        public List<IndicatorResult> Results
        {
            get
            {
                return ResultList.List.Select(p=> ToValue(p)).ToList();
            }
        }

        protected virtual List<IQuote> CreateUsedBars()
        {
            return InputBars.LastItems(Lookback);
        }

        public IndicatorBase(FinanceBars bars, CandlePart candle = CandlePart.Close, FinanceList<R> initialResults = null)
        {            
            Candle = candle;
            InputBars = bars;            
            ResultList = initialResults == null ? new FinanceList<R>(0, null): initialResults;
            bars.ListEvent += BarsChanged;

        }

        protected virtual void BarsChanged(object sender, ListEventArgs<IQuote> e)
        {
            if (e.Action == ListAction.Cleared) usedBars.Clear();
            else if (e.Action == ListAction.ItemRemoved)
            {
               CreateUsedBars();
            } else if (e.Action == ListAction.ItemAdded)
            {
                UsedInput.Add(e.Item);
            }
        }

        public IndicatorBase()
        {

        }

        public abstract decimal NextValue(decimal newVal);
        public abstract R NextResult(IQuote quote);
        //public abstract decimal NextValue(R result);
    }
}
