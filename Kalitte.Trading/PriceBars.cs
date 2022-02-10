// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{

    public interface IValue
    {
        public decimal? Value { get; set; }
    }


    public interface IQuote
    {
        DateTime Date { get; set; }
        decimal Open { get; set; }
        decimal High { get; set; }
        decimal Low { get; set; }
        decimal Close { get; set; }
        decimal Volume { get; set; }
    }

    public class Quote : IQuote, IValue
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal? Value { get => Close; set  { Close = value.Value; } }

        public override string ToString()
        {
            return $"d: {Date}, o:{Open}, h: {High}, l: {Low}, c: {Close}";
        }

        public Quote()
        {

        }

        public Quote(DateTime date, decimal close)
        {
            this.Date = date;
            this.Close = close;
        }

        public Quote(decimal close) : this(DateTime.Now, close)
        {

        }
    }

    public enum ListAction
    {
        ItemAdded = 1,
        ItemRemoved = 2,
        Cleared = 4
    }

    public class ListEventArgs<T>
    {
        public ListAction Action { get; set; }
        public T Item { get; set; }
    }

    public enum CandlePart
    {
        Open,
        High,
        Low,
        Close,
        Volume,
        HL2
    }

    public interface IResult
    {
        DateTime Date { get; }
    }

    [Serializable]
    public class MacdResult : ResultBase
    {
        public decimal? Macd { get; set; }
        public decimal? Signal { get; set; }
        public decimal? Histogram { get; set; }

        // extra interim data
        public decimal? FastEma { get; set; }
        public decimal? SlowEma { get; set; }
    }

    public class EmaResult : ResultBase
    {
        public decimal? Ema { get; set; }
    }

    [Serializable]
    public abstract class ResultBase : IResult
    {
        public DateTime Date { get; set; }
    }

    [Serializable]
    internal class BasicD
    {
        internal DateTime Date { get; set; }
        internal double Value { get; set; }
    }

    public class FinanceList<T>
    {
        protected List<T> items;
        protected ReaderWriterLock rwl = new ReaderWriterLock();
        protected int timeOut = -1;
        public int QueSize { get; private set; } = 0;
        public event EventHandler<ListEventArgs<T>> ListEvent;

        protected virtual List<T> createList(IEnumerable<T> initial = null)
        {
            return initial == null ? new List<T>() : new List<T>(initial);
        }


        public FinanceList(int size, IEnumerable<T> initial = null)
        {
            QueSize = size;
            items = createList();
        }


        public T[] List
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return items.ToArray();
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }

        public T Last
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return items[items.Count - 1];
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }

        public int Count
        {
            get
            {
                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return items.Count;
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }



        public void Push(T item)
        {
            rwl.AcquireWriterLock(timeOut);
            try
            {
                if (QueSize > 0 && items.Count == QueSize)
                {
                    var existing = items[0];
                    items.RemoveAt(0);
                    if (ListEvent != null) ListEvent(this, new ListEventArgs<T>() { Action = ListAction.ItemRemoved, Item = existing });
                }
                items.Add(item);
                if (ListEvent != null)
                {
                    ListEvent(this, new ListEventArgs<T>() { Action = ListAction.ItemAdded, Item = item });
                }

            }
            finally
            {

                rwl.ReleaseWriterLock();
            }
        }

        public void Clear()
        {
            rwl.AcquireWriterLock(timeOut);
            try
            {
                items.Clear();
                if (ListEvent != null) ListEvent(this, new Trading.ListEventArgs<T>() { Action = ListAction.Cleared, Item = default(T) });
            }
            finally
            {

                rwl.ReleaseWriterLock();
            }
        }



    }

    public class IndicatorResults: FinanceList<decimal?>
    {
        public IndicatorResults(int size) : base(size)
        {

        }

        public IndicatorResults() : this(0)
        {

        }
    }

    public class PriceBars : FinanceList<IQuote>
    {
        public CandlePart Ohlc { get; set; } = CandlePart.Close;
        public int DefaultLookback { get; set; }

        public PriceBars(int size): base(size)
        {

        }

        public PriceBars() : this(0)
        {

        }


        private List<BasicD> ConvertToBasic()
        {
            var res = new List<BasicD>();
            try
            {
                //rwl.AcquireReaderLock(timeOut);


                foreach (var item in this.items)
                {
                    decimal val = 0;
                    switch (Ohlc)
                    {
                        case CandlePart.Close: val = item.Close; break;
                        case CandlePart.Volume: val = item.Volume; break;
                        case CandlePart.Open: val = item.Open; break;
                        case CandlePart.High: val = item.High; break;
                        case CandlePart.Low: val = item.Low; break;

                    }

                    res.Add(new BasicD() { Date = item.Date, Value = (double)val });
                }
            }
            finally
            {
                //rwl.ReleaseReaderLock();
            }



            return res.OrderBy(x => x.Date).ToList();
        }


        public decimal[] Values
        {
            get
            {

                //rwl.AcquireReaderLock(timeOut);
                try
                {
                    return items.Select(p =>
                    {
                        switch (Ohlc)
                        {
                            case CandlePart.Close: return p.Close;
                            case CandlePart.Volume: return p.Volume;
                            case CandlePart.Open: return p.Open;
                            case CandlePart.High: return p.High;
                            case CandlePart.Low: return p.Low;
                            default: return 0;

                        }
                    }).ToArray();

                }
                finally
                {
                    //rwl.ReleaseReaderLock();
                }

            }
        }




        public double EmaNext(double price, double lastEma, int lookbackPeriods)
        {
            double k = 2D / (lookbackPeriods + 1);
            double ema = lastEma + (k * (price - lastEma));
            return ema;
        }






        public List<EmaResult> Ema(int lookbackPeriods)
        {
            int length = this.Count;
            var results = new List<EmaResult>(length);
            List<BasicD> bdList = ConvertToBasic();

            double k = 2d / (lookbackPeriods + 1);
            double? lastEma = 0;
            int initPeriods = Math.Min(lookbackPeriods, length);

            for (int i = 0; i < initPeriods; i++)
            {
                lastEma += bdList[i].Value;
            }

            lastEma /= lookbackPeriods;

            // roll through quotes
            for (int i = 0; i < length; i++)
            {
                BasicD h = bdList[i];
                int index = i + 1;

                EmaResult result = new EmaResult()
                {
                    Date = h.Date
                };

                if (index > lookbackPeriods)
                {
                    double? ema = EmaNext(h.Value, lastEma.Value, lookbackPeriods);// lastEma + (k * (h.Value - lastEma));
                    result.Ema = (decimal?)ema;
                    lastEma = ema;
                }
                else if (index == lookbackPeriods)
                {
                    result.Ema = (decimal?)lastEma;
                }

                results.Add(result);
            }

            return results;


            //var emaArray = new List<EmaResult>();
            //double k = 2D / (lookbackPeriods + 1);
            //var data = Values;

            //if (lookbackPeriods <= 0) lookbackPeriods = data.Length;

            //int initPeriods = Math.Min(lookbackPeriods, data.Length);

            //double? lastEma = 0;

            //for (int i = 0; i < initPeriods; i++)
            //{
            //    lastEma += (double)data[i];
            //}

            //lastEma /= lookbackPeriods;

            //decimal? result = null;

            //for (var i = 0; i < data.Length; i++)
            //{
            //    int index = i + 1;

            //    var result = new EmaResult()
            //    {
            //        Date = h.Date
            //    };

            //    if (index > lookbackPeriods)
            //    {
            //        result = EmaNext(data[i], (decimal)lastEma, lookbackPeriods); // + (k * ((double)data[i] - lastEma));
            //        //result = (decimal)ema;
            //        lastEma = (double)result;
            //    }
            //    else if (index == lookbackPeriods)
            //    {
            //        result = (decimal)lastEma;
            //    }

            //    emaArray.Add(result);
        }





        public decimal Cross(decimal baseVal)
        {
            var list = Values;
            var i = list.Length;

            while (--i >= 1)
            {
                decimal cdif = list[i] - baseVal;
                decimal pdif = list[i - 1] - baseVal;
                if (cdif > 0 && pdif < 0) return cdif;
                else if (cdif < 0 && pdif > 0) return cdif;
            }
            return 0;
        }

    }
}
