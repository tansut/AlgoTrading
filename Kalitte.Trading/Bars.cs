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

    public enum BarActions
    {
        BarCreated = 1,
        BarRemoved = 2,
        Cleared = 4
    }

    public class BarEvent
    {
        public BarActions Action { get; set; }
        public IQuote Item { get; set; }
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

    public class Bars
    {
        public event EventHandler<BarEvent> BarEvent;
        private List<IQuote> data;
        public int Size { get; private set; } = 0;
        private int timeOut = -1;
        public CandlePart Ohlc { get; set; } = CandlePart.Close;

        public int DefaultLookback { get; set; }

        //List<decimal> EmaResults = new List<decimal>();

        private ReaderWriterLock rwl = new ReaderWriterLock();

        public Bars(int size = 0)
        {
            Size = size;
            data = new List<IQuote>(size);
        }

        public Bars(IEnumerable<Quote> init)
        {
            data = new List<IQuote>(init);
        }

         List<BasicD> ConvertToBasic()    
        {
            var res = new List<BasicD>();
            try
            {
                rwl.AcquireReaderLock(timeOut);
                

                foreach (var item in this.data)
                {
                    decimal val = 0;
                    switch (Ohlc)
                    {
                        case CandlePart.Close: val = item.Close; break;
                        case CandlePart.Volume: val = item.Volume; break;
                        case CandlePart.Open: val = item.Open; break;
                        case CandlePart.High: val = item.High;break;
                        case CandlePart.Low: val = item.Low;break;

                    }

                    res.Add(new BasicD() { Date = item.Date, Value = (double)val });
                }
            } finally
            {
                rwl.ReleaseReaderLock();
            }
            


            return res.OrderBy(x => x.Date).ToList();
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

        public IQuote Last
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return data[data.Count-1];
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
        }

        public IQuote Latest
        {
            get
            {

                rwl.AcquireReaderLock(timeOut);
                try
                {
                    return data.Last();
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
                    return data.Select(p =>
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
                    var item = data[0];
                    data.RemoveAt(0);
                    if (BarEvent != null) BarEvent(this, new Trading.BarEvent() { Action = BarActions.BarRemoved, Item = item });
                }
                data.Add(quote);
                if (BarEvent != null) BarEvent(this, new Trading.BarEvent() { Action = BarActions.BarCreated, Item = quote });

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
                if (BarEvent != null) BarEvent(this, new Trading.BarEvent() { Action = BarActions.Cleared, Item = null });
            }
            finally
            {

                rwl.ReleaseWriterLock();
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

            //EmaResults = emaArray;

            //return emaArray;
        //}




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
