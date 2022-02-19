using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class Backtest
    {

        public void ApplyProperties(Dictionary<string, object> values)
        {
            var properties = Algo.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(AlgoParam), true));
            foreach (var item in properties)
            {
                object val;
                if (values.TryGetValue(item.Name, out val))
                {
                    var propValue = val;
                    if (val.GetType() != item.PropertyType)
                    {
                        var tc = new TypeConverter();
                        propValue = tc.ConvertTo(val, item.PropertyType);
                    }
                    Algo.GetType().GetProperty(item.Name).SetValue(Algo, propValue);
                }                
            }
        }

        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public AlgoBase Algo { get; set; }

        public Backtest(AlgoBase algo, DateTime start, DateTime end, Dictionary<string, object> initValues = null)
        {
            Algo = algo;
            StartTime = start;
            FinishTime = end;            
            Algo.Simulation = true;
            Algo.UseVirtualOrders = true;
            if (initValues != null) ApplyProperties(initValues);

            //Algo.Symbol = "F_XU0300222";
            //Algo.LoggingLevel = LogLevel.Verbose;
            //Algo.SymbolPeriod = BarPeriod.Min10;
            Algo.Init();
        }

        public void Run(DateTime t1, DateTime t2, bool firstRun = false)
        {
            var seconds = Algo.GetSymbolPeriodSeconds(Algo.SymbolPeriod.ToString());

            Algo.AlgoTime = t1;

             for (var p = t1; p < t2;)
            {

                Algo.Log($"Running backtest for period: {Algo.PeriodBars.Last}", LogLevel.Verbose);

                for (var i = 0; i < seconds; i++)
                {
                    var time = Algo.AlgoTime;
                    foreach (var signal in Algo.Signals)
                    {
                        var result = signal.Check(time);
                    }
                    Algo.CheckDelayedOrders(time);
                    Algo.AlgoTime = Algo.AlgoTime.AddSeconds(1);
                }
                Algo.simulationCount++;

                var bd = Algo.GetPeriodBars(Algo.Symbol, p).Last;
                var newQuote = new MyQuote() { Date = p, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                Algo.PeriodBars.Push(newQuote);
                Algo.Log($"Pushed new bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Verbose);

                p = p.AddSeconds(seconds);
                Algo.AlgoTime = p;
            }

        }


        Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>> GetDates(DateTime t)
        {
            var m1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var m2 = new DateTime(t.Year, t.Month, t.Day, 18, 10, 1);

            var n1 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 0);
            var n2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 1);

            return new Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>>(
                   new Tuple<DateTime, DateTime>(m1, m2), new Tuple<DateTime, DateTime>(n1, n2)
            );
            
        }

        public void Start()
        {
            var days = FinishTime - StartTime;


            for(var d = 0; d <= days.Days; d++)
            {
                var currentDay = StartTime.AddDays(d);
                var periods = this.GetDates(currentDay);
                var prevDayLastBar = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day).AddDays(-1).AddHours(22).AddMinutes(50);

                if (d == 0)
                {                    
                    Algo.LoadBars(Algo.Symbol, prevDayLastBar);
                    Algo.InitMySignals(Algo.AlgoTime);
                    Algo.InitCompleted();
                } else
                {
                    var bd = Algo.GetPeriodBars(Algo.Symbol, prevDayLastBar).Last;
                    var newQuote = new MyQuote() { Date = prevDayLastBar, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                    Algo.PeriodBars.Push(newQuote);
                    Algo.Log($"Pushed new day bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Verbose);
                }
                Run(periods.Item1.Item1, periods.Item1.Item2);
                Run(periods.Item2.Item1, periods.Item2.Item2);
                if (d < days.Days)
                {
                    Algo.Signals.ForEach(p=>p.Reset());
                }
            }
            Algo.Stop();
        }

    }
}
