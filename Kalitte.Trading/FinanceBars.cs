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

    [Serializable]
    public class MyQuote : Quote 
    {
        //public decimal? Value { get => Close; set { Close = value.Value; } }

        public override string ToString()
        {
            return $"d: {Date} o: {Open} h: {High} l: {Low} c:{Close} v:{Volume}";
        }

        public MyQuote(): base()
        {

        }

        public static MyQuote Create(DateTime time, decimal value, OHLCType candle)
        {
            var q = new MyQuote();
            q.Date = time;
            q.High = value;
            q.Low = value;
            q.Open = value;
            if (candle == OHLCType.Volume)
                q.Volume = value;
            else q.Close = value;
            return q;
        }


        public static MyQuote Create(MyQuote source)
        {
            return new MyQuote()
            {
                Date = source.Date,
                Close = source.Close,
                Volume = source.Volume,
                High = source.High,
                Low = source.Low,
                Open = source.Open
            };
        }



        public void Set(decimal value, OHLCType candle)
        {
            switch (candle)
            {
                case OHLCType.Close: { this.Close = value; break; }
                case OHLCType.Volume: { this.Volume = value; break; }
                case OHLCType.Open: { this.Open = value; break; }
                case OHLCType.High: { this.High = value; break; }
                case OHLCType.Low: { this.Low = value; break; }
            }
        }


        public decimal Get(OHLCType ohlc)
        {
            switch (ohlc)
            {
                case OHLCType.Volume: { return this.Volume; }
                case OHLCType.Open: { return this.Open; }
                case OHLCType.High: { return this.High; }
                case OHLCType.Low: { return this.Low; }
                case OHLCType.Close: { return this.Close; }
                case OHLCType.HL2: { return (this.High + this.Low) / 2; }
                case OHLCType.HLC3: { return (this.High + this.Low + this.Close) / 3; }
                case OHLCType.HL2C4: { return (this.High + this.Low + 2*this.Close) / 4; }
                default: { return this.Close; }
            }
        }

        internal void Update(decimal value, OHLCType candle)
        {
            if (value > this.High) this.High = value;
            if (value < this.Low) this.Low = value;
            Set(value, candle);
        }
    }

    [Serializable]
    public class FinanceList<T>
    {
        protected List<T> items;
        protected ReaderWriterLock rvl = new ReaderWriterLock();
        protected int timeOut = -1;
        public int QueSize { get; private set; } = 0;
        public event EventHandler<ListEventArgs<T>> ListEvent;

        protected virtual List<T> createList(IList<T> initial = null)
        {
            return initial == null ? new List<T>() : new List<T>(initial);
        }

        public void ReaderLock()
        {
            rvl.AcquireReaderLock(timeOut);
        }

        public void RelaseReader()
        {
            rvl.ReleaseReaderLock();
        }

        public void WriterLock()
        {
            rvl.AcquireWriterLock(timeOut);
        }

        public void RelaseWriter()
        {
            rvl.ReleaseWriterLock();
        }

        public void Resize(int newSize)
        {
            rvl.AcquireWriterLock(timeOut);
            try
            {
                if (items.Count == newSize) return;
                if (this.items.Count > newSize)
                {
                    var oldItems = this.items.GetRange(0, newSize);
                    items.Clear();
                    //if (ListEvent != null) ListEvent(this, new Trading.ListEventArgs<T>() { Action = ListAction.Cleared, Item = default(T) });
                    this.items.AddRange(oldItems);
                }
                QueSize = newSize;
            }
            finally
            {

                rvl.ReleaseWriterLock();
            }
        }

        public int FindIndex(Predicate<T> match)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                return items.FindIndex(match);
            }
            finally
            {
                rvl.ReleaseReaderLock();
            }

        }


        public FinanceList(int size, IList<T> initial = null)
        {
            QueSize = size;
            items = createList(initial);
        }

        public FinanceList() : this(0)
        {

        }

        public List<T> LastItems(int n)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                return items.Skip(Math.Max(0, items.Count - n)).ToList();
            }
            finally
            {
                rvl.ReleaseReaderLock();
            }
        }


        public List<T> Skip(int n)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                return items.Skip(n).ToList();
            }
            finally
            {
                rvl.ReleaseReaderLock();
            }
        }


        public IList<T> List
        {
            get
            {

                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return items.ToList();
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }

        //public List<T> AsList
        //{
        //    get
        //    {

        //        rvl.AcquireReaderLock(timeOut);
        //        try
        //        {
        //            return new List<T>(items);
        //        }
        //        finally
        //        {
        //            rvl.ReleaseReaderLock();
        //        }
        //    }
        //}

        public T[] ToArray
        {
            get
            {

                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return items.ToArray();
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }

        public T First
        {
            get
            {

                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return items[0];
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }



        public Tuple<T, T> FirstLast
        {
            get
            {

                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return new Tuple<T, T>(items[0], items[items.Count - 1]);
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }

        public T Last
        {
            get
            {

                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return items.Count == 0 ? default(T): items[items.Count - 1];
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }

        public int Count
        {
            get
            {
                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return items.Count;
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }



        public void Push(T item)
        {
            ListEventArgs<T> evtRemove = null, evtAdd = null;
            rvl.AcquireWriterLock(timeOut);
            try
            {
                if (QueSize > 0 && items.Count == QueSize)
                {
                    var existing = items[0];
                    items.RemoveAt(0);
                    evtRemove = new ListEventArgs<T>() { Action = ListAction.ItemRemoved, Item = existing };
                }
                items.Add(item);
                evtAdd = new ListEventArgs<T>() { Action = ListAction.ItemAdded, Item = item };              
            }
            finally
            {
                rvl.ReleaseWriterLock();
            }

            if (ListEvent != null)
            {
                if (evtRemove != null) ListEvent(this, evtRemove);
                if (evtAdd != null) ListEvent(this, evtAdd);
            }
        }

        public bool IsFull
        {
            get
            {
                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return this.Count >= QueSize;
                } finally
                {
                    rvl.ReleaseReaderLock();
                }                
            }
        }

        public void Clear()
        {
            rvl.AcquireWriterLock(timeOut);
            try
            {
                items.Clear();
                if (ListEvent != null) ListEvent(this, new Trading.ListEventArgs<T>() { Action = ListAction.Cleared, Item = default(T) });
            }
            finally
            {

                rvl.ReleaseWriterLock();
            }
        }

        public T GetItem(int index)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                try
                {
                    return items[index];
                }
                catch
                {
                    return default(T);
                }

            }
            finally
            {

                rvl.ReleaseLock();
            }
        }

    }

    [Serializable]
    public class FinanceBars : FinanceList<IQuote>
    {
        public string Symbol { get; private set; }
        public BarPeriod Period { get; private set; }
        public int RecommendedSkip { get; set; } = 0;
        protected ReaderWriterLock cvl = new ReaderWriterLock();
        private MyQuote current = null;

        public MyQuote Current
        {
            get
            {
                cvl.AcquireReaderLock(timeOut);
                try
                {
                    return current;
                }
                finally
                {
                    cvl.ReleaseReaderLock();
                }
            }
            private set
            {
                cvl.AcquireWriterLock(timeOut);
                try
                {
                    current = value;
                }
                finally
                {
                    cvl.ReleaseWriterLock();
                }
            }
        }

        public void ClearCurrent()
        {
            Current = null;
        }

        public IQuote SetCurrent(DateTime date, decimal close, decimal? volume = null)
        {
            cvl.AcquireWriterLock(timeOut);
            try
            {
                if (current == null)
                    current = new MyQuote();
                current.Date = date;
                current.Close = close;
                current.Volume = volume.HasValue ? volume.Value: Current.Volume;
                current.Low = close < Current.Low ? close : Current.Low;
                current.High = close > Current.High ? close : Current.Low;
                Current.Open = Current.Open == 0 ? close: Current.Open;
                return Current;
            }
            finally 
            {
                cvl.ReleaseWriterLock();
            }            
        }

        public FinanceBars(string symbol, BarPeriod period, int maxSize = 0) : base(maxSize)
        {
            this.Symbol = symbol;
            this.Period = period;
        }

        public FinanceBars(string symbol, BarPeriod period) : this(symbol, period, 0)
        {

        }

        public List<IQuote> RecommendedItems
        {
            get
            {
                rvl.AcquireReaderLock(timeOut);
                try
                {
                    return this.Skip(RecommendedSkip);
                }
                finally
                {
                    rvl.ReleaseReaderLock();
                }
            }
        }


        public decimal[] Values(CandlePart candle)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                return items.Select(p =>
                {
                    switch (candle)
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
                rvl.ReleaseReaderLock();
            }


        }




        public static double EmaNext(double price, double lastEma, int lookbackPeriods)
        {
            double k = 2D / (lookbackPeriods + 1);
            double ema = lastEma + (k * (price - lastEma));
            return ema;
        }



        public decimal derivative(int toBack, int from = 0)
        {
            rvl.AcquireReaderLock(timeOut);
            try
            {
                var count = this.items.Count;
                var lastIndex = count - from;
                var firstIndex = Math.Max(lastIndex - toBack, 0);
                return (this.items[lastIndex].Close - this.items[firstIndex].Close) / toBack;
            }
            finally
            {
                rvl.ReleaseReaderLock();
            }
        }




    }
}
