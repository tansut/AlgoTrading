// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.Indicators
{
    public abstract class IndicatorBase
    {
        protected System.Timers.Timer _timer = null;
        private static object _locker = new object();
        public string Name { get; set; }
        public Kalitte.Trading.Algos.AlgoBase Algo { get; set; }
        public bool Enabled { get; set; }
        public bool TimerEnabled { get; set; }
        public bool Simulation { get; set; }
        public string Symbol { get; private set; }

        public Bars InputBars { get; }
        public Bars ResultBars { get; set; } = null;

        public bool HasResult => ResultBars.Count > 0 && ResultBars.List.Last().Close > 0;

        public IndicatorBase(Bars bars)
        {
            //this.Algo = Algo;
            InputBars = bars;
        }

        public abstract Quote CreateNewResultBar(IQuote newBar);

        //public abstract List<decimal> Values
        //{
        //    get;
        //}
        //public abstract decimal LastValue(decimal newValue);
    }
}
