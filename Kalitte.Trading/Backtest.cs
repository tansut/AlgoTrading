using Kalitte.Trading.Algos;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class Backtest
    {



        public AlgoBase Algo { get; set; }

        public Backtest(AlgoBase algo, DateTime start, DateTime end)
        {
            Algo = algo;
            algo.TestStart = start;
            algo.TestFinish = end;
            Algo.Simulation = true;
            Algo.UseVirtualOrders = true;            
            Algo.Init();
        }

        public void Run(DateTime t1, DateTime t2, bool firstRun = false)
        {
                  
            var secDict = new Dictionary<int, BarPeriod>();
            var secondsToStop = Algo.Symbols.Select(p => secDict[Algo.GetSymbolPeriodSeconds(p.Periods.Period.ToString())] = p.Periods.Period).ToList();
            var periodBarIndexes = new Dictionary<int, int>();
            var periodBars = new Dictionary<int, FinanceBars>();
            var seconds = 0;


            foreach (var symbol in Algo.Symbols)
            {
                var bars = Algo.GetPeriodBars(symbol.Symbol, symbol.Periods.Period);
                var sec = Algo.GetSymbolPeriodSeconds(symbol.Periods.Period.ToString());
                periodBars.Add(sec, bars);
            }

            var periodBarsLoaded = true;

            for (var p = t1; p < t2;)
            {
                if (p >= DateTime.Now) break;
                if (periodBarsLoaded)
                {
                    Algo.AlgoTime = p;

                    Func<object, SignalResult> action = (object stateo) =>
                    {
                        var state = (Dictionary<string, object>)stateo;
                        DateTime time = (DateTime)(state["time"]);
                        return ((Signal)state["signal"]).Check(time);
                    };

                    var time = Algo.AlgoTime;
                    var tasks = new List<Task<SignalResult>>();

                    foreach (var signal in Algo.Signals)
                    {
                        var dict = new Dictionary<string, object>();
                        dict["time"] = time;
                        dict["signal"] = signal;
                        tasks.Add(Task<SignalResult>.Factory.StartNew(action, dict));
                    }
                    Task.WaitAll(tasks.ToArray());
                    Algo.CheckDelayedOrders(time);
                    Algo.simulationCount++;
                }

                seconds++;                

                foreach (var sec in secDict)
                {
                    if (seconds % sec.Key == 0)
                    {
                        IQuote period = null;
                        var round = Helper.RoundDown(p, TimeSpan.FromSeconds(sec.Key));
                        if (!periodBarIndexes.ContainsKey(sec.Key)) {                                                                           
                            var allItems = periodBars[sec.Key].AsList;
                            periodBarIndexes[sec.Key] = allItems.FindIndex(i => i.Date == round);
                        }
                        period = periodBars[sec.Key].GetItem(periodBarIndexes[sec.Key]);

                        if (period == null || period.Date != round)
                        {
                            Algo.Log($"Error loading period for {round}", LogLevel.Error, p);
                            periodBarsLoaded = false;
                        }
                        else
                        {
                            Algo.PushNewBar(Algo.Symbol, sec.Value, period);
                            periodBarIndexes[sec.Key] = periodBarIndexes[sec.Key] + 1;
                            periodBarsLoaded = true;
                        }
                    }
                }

                p = p.AddSeconds(1);

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
            var days = Algo.TestFinish.Value - Algo.TestStart.Value;


            for (var d = 0; d <= days.Days; d++)
            {
                var currentDay = Algo.TestStart.Value.AddDays(d);

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
                    Algo.Signals.ForEach(p => p.Reset());
                }
                Run(periods.Item1.Item1, periods.Item1.Item2);
                Run(periods.Item2.Item1, periods.Item2.Item2);
            }
            Algo.Stop();
        }

    }


    public class Optimizer<T> where T: AlgoBase
    {
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }

        Type algoType;

        public Optimizer(DateTime start, DateTime finish, Type algoType)
        {
            this.StartTime = start;
            this.FinishTime = finish;
            this.algoType = algoType;
        }

        public void Start(AlternateValues alternates)
        {
            var cases = alternates.GenerateTestCases();
            Console.WriteLine($" ** WILL RUN {cases.Count} TESTS ** Hit to continue ...");
            //Console.ReadKey();
            RandomGenerator random = new RandomGenerator();
            var resultFile = $"c:\\kalitte\\log\\simulation\\results\\br-{StartTime.ToString("yyyy-MM-dd")}-{FinishTime.ToString("yyyy-MM-dd")}-{(random.Next(1000000, 9999999))}.txt";
            Console.WriteLine($"Saving tests to {resultFile}");
            var completed = 0;
            CreateHeaders(resultFile);
            Parallel.For(0, cases.Count, i =>
            {
                var initValues = cases[i];                
                var algo = (AlgoBase)Activator.CreateInstance(typeof(T), new Object[] { initValues });
                algo.SimulationFile = resultFile;
                Backtest test = new Backtest(algo, this.StartTime, this.FinishTime);

                //Console.WriteLine($"Running test case {i}/{cases.Count} for {algo.InstanceName} using {algo.LogFile}");
                try
                {
                    test.Start();
                    Console.WriteLine($"Completed case {algo.InstanceName}[{++completed}/{cases.Count}]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in instance {algo.InstanceName}. {ex}");
                }
            });
            Console.WriteLine(" ** COMPLETED ** Hit to close ...");
            Console.ReadKey();
        }

        private void CreateHeaders(string resultFile)
        {
            var dictionary = AlgoBase.GetProperties(typeof(T));
            var sb = new StringBuilder();
            foreach (var key in dictionary.Keys) sb.Append(key +"\t");
            //F_XU0300222: long/ 1 / Cost: 2250.75 Total: 2250.75 PL: -32.25 Commission: 39.15 NetPL: -71.40
            sb.Append("Pos\tQuantity\tCost\tTotal\tPL\tCommission\tNetPL\tOrdertotal\tLog\t\n");
            File.WriteAllText(resultFile, sb.ToString());
        }
    }
}
