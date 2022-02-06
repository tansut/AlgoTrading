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

        public Quote(decimal close): this(DateTime.Now, close)
        {

        }
    }

    public class Bars
    {
        private Queue<IQuote> data;
        public int Size { get; private set; }
        private int timeOut = -1;

        private double alpha = 0.2;

        private ReaderWriterLock rwl = new ReaderWriterLock();

        public Bars(int size)
        {
            Size = size;
            alpha = 2D / (size + 1);
            data = new Queue<IQuote>(size);
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

        public decimal[] CloseList
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return data.Select(p => p.Close).ToArray();
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
                if (data.Count == Size)
                        data.Dequeue();
                data.Enqueue(quote);
            }
            finally
            {
                
                rwl.ReleaseWriterLock();
            }
        }

        public List<decimal> Ema(int lookbackPeriods = 0)
        {
            List<decimal> emaArray = new List<decimal>();
            double k = 2D / (lookbackPeriods + 1);
            var data = CloseList;
            
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
                    double ema = lastEma + (k * ((double)data[i] - lastEma));
                    result = (decimal)ema;
                    lastEma = ema;
                }
                else if (index == lookbackPeriods)
                {
                    result = (decimal)lastEma;
                }

                emaArray.Add(result);
            }

            return emaArray;
        }


        public decimal Cross(decimal baseVal)
        {
            var list = CloseList;
            var i = list.Length;

            while(--i >= 1)
            {
                decimal cdif = list[i] - baseVal;
                decimal pdif = list[i-1] - baseVal;
                if (cdif > 0 && pdif < 0) return cdif;
                else if (cdif < 0 && pdif > 0) return cdif;                
            }
            return 0;
        }

    }
}
