// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{

    public interface IQuote
    {
        DateTime Date { get; set; }
        decimal Open { get; set; }
        decimal High { get; set; }
        decimal Low { get; set; }
        decimal Close { get; set; }
        decimal Volume { get; set; }
    }

    public class Quote : IQuote
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

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

    public class Bars
    {
        private Queue<IQuote> data;
        public int Size { get; private set; } = 0;
        private int timeOut = -1;
        public OHLC Ohlc { get; set; } = OHLC.Close;

        public int DefaultLookback { get; set; }

        //List<decimal> EmaResults = new List<decimal>();

        private ReaderWriterLock rwl = new ReaderWriterLock();

        public Bars(int size = 0)
        {
            Size = size;
            data = new Queue<IQuote>(size);
        }

        public Bars(IEnumerable<Quote> init)
        {        
            data = new Queue<IQuote>(init);
        }

        public IQuote[] List
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return data.ToArray();
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }


        public decimal[] Values
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return data.Select(p => { switch (Ohlc)
                        {
                            case OHLC.Close: return p.Close;
                                case OHLC.Volume: return p.Volume;
                                case OHLC.Open: return p.Open;
                                case OHLC.High: return p.High;
                                case OHLC.Low: return p.Low;
                                default : return 0;

                        } }).ToArray();
                    
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
                    return data.Count;
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }

            }
        }




        public void Push(IQuote quote)
        {
            rwl.AcquireWriterLock(timeOut);
            try
            {
                if (Size > 0 && data.Count == Size)
                {
                    data.Dequeue();
                    //EmaResults.RemoveAt(0);
                }
                data.Enqueue(quote);
                //var current = data.Dequeue();
                //EmaResults.Add(EmaNext(quote.Close, 0, DefaultLookback));
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
                data.Clear();
                //EmaResults.Clear();
            }
            finally
            {

                rwl.ReleaseWriterLock();
            }
        }



        public decimal EmaNext(decimal price, decimal lastEma, int lookbackPeriods)
        {
            double k = 2D / (lookbackPeriods + 1);
            double ema = (double)lastEma + (k * ((double)price - (double)lastEma));
            return (decimal)ema;
        }

        public List<decimal> Ema(int lookbackPeriods = 0)
        {
            List<decimal> emaArray = new List<decimal>();
            double k = 2D / (lookbackPeriods + 1);
            var data = Values;

            if (lookbackPeriods <= 0) lookbackPeriods = data.Length;

            int initPeriods = Math.Min(lookbackPeriods, data.Length);

            double lastEma = 0;

            for (int i = 0; i < initPeriods; i++)
            {
                lastEma += (double)data[i];
            }

            lastEma /= lookbackPeriods;

            decimal result = 0;

            for (var i = 0; i < data.Length; i++)
            {
                int index = i + 1;

                if (index > lookbackPeriods)
                {
                    result = EmaNext(data[i], (decimal)lastEma, lookbackPeriods); // + (k * ((double)data[i] - lastEma));
                    //result = (decimal)ema;
                    lastEma = (double)result;
                }
                else if (index == lookbackPeriods)
                {
                    result = (decimal)lastEma;
                }

                emaArray.Add(result);
            }

            //EmaResults = emaArray;

            return emaArray;
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
