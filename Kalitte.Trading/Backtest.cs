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
            Algo.Init();
        }

        public void Run(DateTime t1, DateTime t2, bool firstRun = false)
        {
            var seconds = Algo.GetSymbolPeriodSeconds(Algo.SymbolPeriod.ToString());

            Algo.AlgoTime = t1;

            for (var p = t1; p < t2;)
            {
                

                Algo.Log($"Running backtest for period: {Algo.PeriodBars.Last}", LogLevel.Debug);

                Func<object, SignalResultX> action = (object stateo) =>
               {
                   var state = (Dictionary<string, object>)stateo;
                   DateTime time = (DateTime)(state["time"]);
                   return ((Signal)state["signal"]).Check(time);

               };

                for (var i = 0; i < seconds; i++)
                {
                    var time = Algo.AlgoTime;
                    //Algo.Log($"Running signals for {time}", LogLevel.Critical, time);
                    var tasks = new List<Task<SignalResultX>>();

                    foreach (var signal in Algo.Signals)
                    {
                        var dict = new Dictionary<string, object>();
                        dict["time"] = time;
                        dict["signal"] = signal;
                        tasks.Add(Task<SignalResultX>.Factory.StartNew(action, dict));
                    }
                    Task.WaitAll(tasks.ToArray());
                    Algo.CheckDelayedOrders(time);
                    Algo.AlgoTime = Algo.AlgoTime.AddSeconds(1);
                }
                Algo.simulationCount++;
                Algo.PushNewBar(Algo.Symbol, Algo.SymbolPeriod, p);

                p = Algo.AlgoTime;
            }

        }


        Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>> GetDates(DateTime t)
        {
            var m1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var m2 = new DateTime(t.Year, t.Month, t.Day, 18, 20, 0);

            var n1 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 0);
            var n2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);

            return new Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>>(
                   new Tuple<DateTime, DateTime>(m1, m2), new Tuple<DateTime, DateTime>(n1, n2)
            );

        }

        public void Start()
        {
            var days = FinishTime - StartTime;


            for (var d = 0; d <= days.Days; d++)
            {
                var currentDay = StartTime.AddDays(d);

                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday) continue;
                if (currentDay >= DateTime.Now) break;
                var periods = this.GetDates(currentDay);
                Algo.AlgoTime = periods.Item1.Item1;
                var prevDayLastBar = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day).AddDays(-1).AddHours(22).AddMinutes(50);

                if (d == 0)
                {
                    Algo.InitializeBars(Algo.Symbol, Algo.SymbolPeriod, prevDayLastBar);
                    Algo.InitMySignals(Algo.AlgoTime);
                    Algo.InitCompleted();
                }
                else
                {
                    //var bd = Algo.GetPeriodBars(Algo.Symbol, prevDayLastBar).Last;
                    //var newQuote = new MyQuote() { Date = prevDayLastBar, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                    //Algo.PeriodBars.Push(newQuote);
                    //Algo.Log($"Pushed new day bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Debug);
                    Algo.Signals.ForEach(p => p.Reset());
                }
                Run(periods.Item1.Item1, periods.Item1.Item2);
                Run(periods.Item2.Item1, periods.Item2.Item2);
            }
            Algo.Stop();
        }

    }
}
