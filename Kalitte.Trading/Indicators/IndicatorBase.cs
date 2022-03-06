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
        IndicatorResult NextValue(decimal? price = null, decimal? volume = null);
        decimal? CurrentValue { get; }
        List<IndicatorResult> Results { get; }
        int Lookback { get; set; }
        BarPeriod Period { get; set; }
        List<IQuote> UsedInput { get; }
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
        public BarPeriod Period { get; set; }
        public CandlePart Candle { get; set; } = CandlePart.Close;
        public FinanceBars InputBars { get; }
        public FinanceList<R> ResultList { get; set; } = null;
        private  List<IQuote> usedBars;
        public int Lookback { get; set; }

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

        protected virtual void CreateResult()
        {
            ResultList.WriterLock();
            try
            {
                ResultList.Clear();
                var results = GetResults();
                foreach (var item in results)
                {
                    ResultList.Push(item);
                }
            }
            finally
            {
                ResultList.RelaseWriter();
            }
            

        }

        public abstract IEnumerable<R> GetResults();

        public List<IndicatorResult> Results
        {
            get
            {
                return ResultList.List.Select(p=> ToValue(p)).ToList();
            }
        }

        protected virtual List<IQuote> CreateUsedBars()
        {
            return InputBars.RecommendedItems;
        }

        public IndicatorBase(FinanceBars bars, CandlePart candle = CandlePart.Close)
        {            
            Candle = candle;
            InputBars = bars;            
            ResultList = new FinanceList<R>(0, null);
            bars.ListEvent += BarsChangedEvent;
        }



        protected virtual void BarsChangedEvent(object sender, ListEventArgs<IQuote> e)
        {
            ResultList.WriterLock();
            try
            {
                if (e.Action == ListAction.Cleared) usedBars.Clear();
                else if (e.Action == ListAction.ItemRemoved)
                {
                    CreateUsedBars();
                }
                else if (e.Action == ListAction.ItemAdded)
                {
                    UsedInput.Add(e.Item);
                }
                CreateResult();
            } finally
            {
                ResultList.RelaseWriter();
            }

        }


        public IndicatorBase()
        {

        }

        public virtual IndicatorResult NextValue(decimal? price = null, decimal? volume = null)
        {
            ResultList.WriterLock();
            try
            {
                IQuote quote = null;
                if (!price.HasValue)
                {
                    quote = InputBars.Current;
                }
                else
                {
                    quote = new MyQuote() { Date = DateTime.Now, Close = price.Value, Volume = volume.HasValue ? volume.Value : 0 };
                }
                var nr = NextResult(quote);
                return ToValue(nr);
            }
            finally
            {
                ResultList.RelaseWriter();                
            }
        }
        public abstract R NextResult(IQuote quote);
    }
}
