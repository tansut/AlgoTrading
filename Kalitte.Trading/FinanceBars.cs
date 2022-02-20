// algo
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
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

    public interface IValue
    {
        decimal? Value { get; set; }
    }


    public class MyQuote : Quote, IValue
    {
        public decimal? Value { get => Close; set { Close = value.Value; } }

        public override string ToString()
        {
            return $"d: {Date} o: {Open} h: {High} l: {Low} c:{Close}";
        }
    }


    public class FinanceList<T>
    {
        protected IList<T> items;
        protected ReaderWriterLock rwl = new ReaderWriterLock();
        protected int timeOut = -1;
        public int QueSize { get; private set; } = 0;
        public event EventHandler<ListEventArgs<T>> ListEvent;

        protected virtual IList<T> createList(IList<T> initial = null)
        {
            return initial == null ? new List<T>() : initial;
        }

        public void Resize(int newSize)
        {
            rwl.AcquireWriterLock(timeOut);
            try
            {
                if (this.items.Count > newSize)
                {
                    items.Clear();
                    if (ListEvent != null) ListEvent(this, new Trading.ListEventArgs<T>() { Action = ListAction.Cleared, Item = default(T) });
                }
                QueSize = newSize;
            }
            finally
            {

                rwl.ReleaseWriterLock();
            }
        }


        public FinanceList(int size, IEnumerable<T> initial = null)
        {
            QueSize = size;
            items = createList();
        }

        public FinanceList() : this(0)
        {

        }

        public List<T> LastItems(int n)
        {
            return items.Skip(Math.Max(0, Count - n)).ToList();
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

        public List<T> AsList
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return new List<T>(items);
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }

        public T First
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return items[0];
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }



        public Tuple<T, T> FirstLast
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return new Tuple<T, T>(items[0], items[items.Count - 1]);
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

    //public class IndicatorResults: FinanceList<IQuote?>
    //{

    //    public IndicatorResults() : base(0)
    //    {

    //    }
    //}

    public class FinanceBars : FinanceList<IQuote>
    {
        public CandlePart Ohlc { get; set; } = CandlePart.Close;


        public FinanceBars(int size) : base(size)
        {

        }

        public FinanceBars() : this(0)
        {

        }




        public decimal[] Values
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
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
                    rwl.ReleaseReaderLock();
                }

            }
        }




        public static double EmaNext(double price, double lastEma, int lookbackPeriods)
        {
            double k = 2D / (lookbackPeriods + 1);
            double ema = lastEma + (k * (price - lastEma));
            return ema;
        }


        //public double EmaNext2(double price, double lastEma, int lookbackPeriods)
        //{
        //    double k = 2D / (lookbackPeriods + 1);
        //    double ema = lastEma + (k * (price - lastEma));
        //    return ema;
        //}

        //public static List<EmaResult> Ema2(List<IValue> input, int lookbackPeriods)
        //{
        //    int length = input.Count;
        //    var results = new List<EmaResult>(length);
        //    List<BasicD> bdList = ConvertToBasic2(input);

        //    double k = 2d / (lookbackPeriods + 1);
        //    double? lastEma = 0;
        //    int initPeriods = Math.Min(lookbackPeriods, length);

        //    for (int i = 0; i < initPeriods; i++)
        //    {
        //        lastEma += bdList[i].Value;
        //    }

        //    lastEma /= lookbackPeriods;

        //    // roll through quotes
        //    for (int i = 0; i < length; i++)
        //    {
        //        BasicD h = bdList[i];
        //        int index = i + 1;

        //        EmaResult result = new EmaResult()
        //        {
        //            Date = h.Date
        //        };

        //        if (index > lookbackPeriods)
        //        {
        //            double? ema = EmaNext2(h.Value, lastEma.Value, lookbackPeriods);// lastEma + (k * (h.Value - lastEma));
        //            result.Ema = (decimal?)ema;
        //            lastEma = ema;
        //        }
        //        else if (index == lookbackPeriods)
        //        {
        //            result.va = (decimal?)lastEma;
        //        }

        //        results.Add(result);
        //    }

        //    return results;


        //    //var emaArray = new List<EmaResult>();
        //    //double k = 2D / (lookbackPeriods + 1);
        //    //var data = Values;

        //    //if (lookbackPeriods <= 0) lookbackPeriods = data.Length;

        //    //int initPeriods = Math.Min(lookbackPeriods, data.Length);

        //    //double? lastEma = 0;

        //    //for (int i = 0; i < initPeriods; i++)
        //    //{
        //    //    lastEma += (double)data[i];
        //    //}

        //    //lastEma /= lookbackPeriods;

        //    //decimal? result = null;

        //    //for (var i = 0; i < data.Length; i++)
        //    //{
        //    //    int index = i + 1;

        //    //    var result = new EmaResult()
        //    //    {
        //    //        Date = h.Date
        //    //    };

        //    //    if (index > lookbackPeriods)
        //    //    {
        //    //        result = EmaNext(data[i], (decimal)lastEma, lookbackPeriods); // + (k * ((double)data[i] - lastEma));
        //    //        //result = (decimal)ema;
        //    //        lastEma = (double)result;
        //    //    }
        //    //    else if (index == lookbackPeriods)
        //    //    {
        //    //        result = (decimal)lastEma;
        //    //    }

        //    //    emaArray.Add(result);
        //}




        //public List<EmaResult> Ema(int lookbackPeriods)
        //{
        //    int length = this.Count;
        //    var results = new List<EmaResult>(length);
        //    List<BasicD> bdList = ConvertToBasic();

        //    double k = 2d / (lookbackPeriods + 1);
        //    double? lastEma = 0;
        //    int initPeriods = Math.Min(lookbackPeriods, length);

        //    for (int i = 0; i < initPeriods; i++)
        //    {
        //        lastEma += bdList[i].Value;
        //    }

        //    lastEma /= lookbackPeriods;

        //    // roll through quotes
        //    for (int i = 0; i < length; i++)
        //    {
        //        BasicD h = bdList[i];
        //        int index = i + 1;

        //        EmaResult result = new EmaResult()
        //        {
        //            Date = h.Date
        //        };

        //        if (index > lookbackPeriods)
        //        {
        //            double? ema = EmaNext(h.Value, lastEma.Value, lookbackPeriods);// lastEma + (k * (h.Value - lastEma));
        //            result.Ema = (decimal?)ema;
        //            lastEma = ema;
        //        }
        //        else if (index == lookbackPeriods)
        //        {
        //            result.Ema = (decimal?)lastEma;
        //        }

        //        results.Add(result);
        //    }

        //    return results;


        //    //var emaArray = new List<EmaResult>();
        //    //double k = 2D / (lookbackPeriods + 1);
        //    //var data = Values;

        //    //if (lookbackPeriods <= 0) lookbackPeriods = data.Length;

        //    //int initPeriods = Math.Min(lookbackPeriods, data.Length);

        //    //double? lastEma = 0;

        //    //for (int i = 0; i < initPeriods; i++)
        //    //{
        //    //    lastEma += (double)data[i];
        //    //}

        //    //lastEma /= lookbackPeriods;

        //    //decimal? result = null;

        //    //for (var i = 0; i < data.Length; i++)
        //    //{
        //    //    int index = i + 1;

        //    //    var result = new EmaResult()
        //    //    {
        //    //        Date = h.Date
        //    //    };

        //    //    if (index > lookbackPeriods)
        //    //    {
        //    //        result = EmaNext(data[i], (decimal)lastEma, lookbackPeriods); // + (k * ((double)data[i] - lastEma));
        //    //        //result = (decimal)ema;
        //    //        lastEma = (double)result;
        //    //    }
        //    //    else if (index == lookbackPeriods)
        //    //    {
        //    //        result = (decimal)lastEma;
        //    //    }

        //    //    emaArray.Add(result);
        //}


        public bool IsFull
        {
            get
            {
                return this.Count >= QueSize;
            }
        }

        public decimal derivative(int toBack, int from = 0)
        {
            rwl.AcquireReaderLock(timeOut);
            try
            {
                var count = this.items.Count;
                var lastIndex = count - from;
                var firstIndex = Math.Max(lastIndex - toBack, 0);                                
                return (this.items[lastIndex].Close - this.items[firstIndex].Close) / toBack;

            }
            finally
            {
                rwl.ReleaseReaderLock();
            }


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
