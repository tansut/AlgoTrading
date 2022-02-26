﻿// algo
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
            return $"d: {Date} o: {Open} h: {High} l: {Low} c:{Close} v:{Volume}";
        }

        public void Set(decimal value, CandlePart candle)
        {
            switch (candle)
            {
                case CandlePart.Close: { this.Close = value; break; }
                case CandlePart.Volume: { this.Volume = value; break; }
                case CandlePart.Open: { this.Open = value; break; }
                case CandlePart.High: { this.High = value; break; }
                case CandlePart.Low: { this.Low = value; break; }                
            }
        }
    }


    public class FinanceList<T>
    {
        protected List<T> items;
        protected ReaderWriterLock rwl = new ReaderWriterLock();
        protected int timeOut = -1;
        public int QueSize { get; private set; } = 0;
        public event EventHandler<ListEventArgs<T>> ListEvent;

        protected virtual List<T> createList(IList<T> initial = null)
        {
            return initial == null ? new List<T>() : new List<T>(initial);
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

        public int FindIndex(Predicate<T> match)
        {
            rwl.AcquireWriterLock(timeOut);
            try
            {
                return items.FindIndex(match);
            }
            finally
            {
                rwl.ReleaseWriterLock();
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
            rwl.AcquireReaderLock(timeOut);
            try
            {
                return items.Skip(Math.Max(0, Count - n)).ToList();
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }           
        }


        public List<T> Skip(int n)
        {
            rwl.AcquireReaderLock(timeOut);
            try
            {
                return items.Skip(n).ToList();
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }
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

        public T[] ToArray
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

        public bool IsFull
        {
            get
            {
                return this.Count >= QueSize;
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

        public T GetItem(int index)
        {
            rwl.AcquireReaderLock(timeOut);
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

                rwl.ReleaseLock();
            }
        }

    }


    public class FinanceBars : FinanceList<IQuote>
    {
        public string Symbol { get; private set; }
        public BarPeriod Period { get; private set; }

        public int RecommendedSkip { get; set; } = 0;


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
                return this.Skip(RecommendedSkip);
            }
        }


        public decimal[] Values(CandlePart candle)
        {


            rwl.AcquireReaderLock(timeOut);
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
                rwl.ReleaseReaderLock();
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




    }
}
