using Kalitte.Trading.Algos;
using Newtonsoft.Json;
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

    public class OptimizerSettings
    {
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public AlternateValues Alternates { get; set; }
        public bool AutoClosePositions { get; set; }
        public int SamplingPerc { get; set; } = 0;

        public static OptimizerSettings LoadFromFile(string fileName)
        {
            var file = File.ReadAllText(fileName);
            var obj = JsonConvert.DeserializeObject<OptimizerSettings>(file);
            return obj;
        }

        public void SaveToFile(string fileName)
        {
            var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented

            });
            File.WriteAllText(fileName, json);
        }
    }

    public class Backtest
    {



        public AlgoBase Algo { get; set; }

        public Backtest RelatedTest { get; set; }
        public bool AutoClosePositions { get; internal set; }

        public Backtest(AlgoBase algo, DateTime start, DateTime end, Backtest related = null)
        {
            Algo = algo;
            algo.TestStart = start;
            algo.TestFinish = end;
            Algo.Simulation = true;
            Algo.UseVirtualOrders = true;
            this.RelatedTest = related;
            AutoClosePositions = false;            
        }
        

        private Dictionary<int, BarPeriod> secDict;
        private List<BarPeriod> secondsToStop;
        private Dictionary<int, FinanceBars> periodBars;

        void createParameters()
        {
            if (RelatedTest != null)
            {
                secDict = RelatedTest.secDict;
                secondsToStop = RelatedTest.secondsToStop;
                periodBars = RelatedTest.periodBars;
            }
            else
            {
                secDict = new Dictionary<int, BarPeriod>();
                secondsToStop = Algo.Symbols.Select(p => secDict[Algo.GetSymbolPeriodSeconds(p.Periods.Period.ToString())] = p.Periods.Period).ToList();
                periodBars = new Dictionary<int, FinanceBars>();
                foreach (var symbol in Algo.Symbols)
                {
                    var bars = Algo.GetPeriodBars(symbol.Symbol, symbol.Periods.Period);
                    var sec = Algo.GetSymbolPeriodSeconds(symbol.Periods.Period.ToString());
                    periodBars.Add(sec, bars);
                }
            }
        }

        public void Run(DateTime t1, DateTime t2)
        {
            var periodBarIndexes = new Dictionary<int, int>();
            var seconds = 0;
            var periodBarsLoaded = true;

            for (var p = t1; p <= t2; p = p.AddSeconds(1))
            {
                if (periodBarsLoaded)
                {
                    if (p > DateTime.Now) break;
                    Algo.SetTime(p);
                    Algo.SetBarCurrentValues();
                    var time = Algo.Now;
                    Algo.CheckBacktestBeforeRun(time);
                    Algo.RunSignals(time);
                    Algo.CheckBacktestAfterRun(time);
                    if (Algo.UsePerformanceMonitor) Algo.Watch.CheckMonitor();
                    Algo.simulationCount++;
                }

                seconds++;

                foreach (var sec in secDict)
                {
                    if (seconds % sec.Key == 0)
                    {
                        IQuote period = null;
                        var round = Helper.RoundDown(p, TimeSpan.FromSeconds(sec.Key));
                        if (!periodBarIndexes.ContainsKey(sec.Key))
                        {
                            periodBarIndexes[sec.Key] = periodBars[sec.Key].FindIndex(i => i.Date == round);
                        }
                        period = periodBars.ContainsKey(sec.Key) ? periodBars[sec.Key].GetItem(periodBarIndexes[sec.Key]) : null;
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



            }

            foreach (var sec in secDict)
            {
                var round = Helper.RoundUp(t2, TimeSpan.FromSeconds(sec.Key));
                var roundDown = Helper.RoundDown(t2, TimeSpan.FromSeconds(sec.Key));
                if (round != t2)
                {
                    var period = periodBars.ContainsKey(sec.Key) && periodBarIndexes.ContainsKey(sec.Key) ? periodBars[sec.Key].GetItem(periodBarIndexes[sec.Key]) : null;
                    if (period == null || period.Date != roundDown)
                    {
                        Algo.Log($"Error loading period for {round}", LogLevel.Error, round);
                    }
                    else
                    {
                        Algo.PushNewBar(Algo.Symbol, sec.Value, period);
                    }
                }
            }
        }


        Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>> GetDates(DateTime t, DateTime s, DateTime f)
        {
            var m1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var m2 = new DateTime(t.Year, t.Month, t.Day, 18, 15, 0);

            var n1 = new DateTime(t.Year, t.Month, t.Day, 19, 0, 0);
            var n2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);

            //if (m1 < s) m1 = s;
            //if (m2 > f) m2 = f;
            ////if (n1 < s) n1 = s;
            //if (n2 > f) n2 = f;

            return new Tuple<Tuple<DateTime, DateTime>, Tuple<DateTime, DateTime>>(
                   new Tuple<DateTime, DateTime>(m1, m2), new Tuple<DateTime, DateTime>(n1, n2)
            );

        }

        public void Start()
        {
            var days = Algo.TestFinish.Value - Algo.TestStart.Value;

            var prevDayLastBar = new DateTime(Algo.TestStart.Value.Year, Algo.TestStart.Value.Month, Algo.TestStart.Value.Day).AddDays(-1).AddHours(22).AddMinutes(50);
            var algoInited = false;


            for (var d = 0; d <= days.Days; d++)
            {
                var currentDay = Algo.TestStart.Value.AddDays(d);
                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday) continue;
                if (currentDay > DateTime.Now) break;
                var periods = this.GetDates(currentDay, Algo.TestStart.Value, Algo.TestFinish.Value);
                if (!algoInited)
                {
                    Algo.SetTime(periods.Item1.Item1);
                    Algo.Init();
                    Algo.InitializeBars(Algo.Symbol, Algo.SymbolPeriod, prevDayLastBar);
                    createParameters();
                    Algo.Start();
                    algoInited = true;
                }
                Run(periods.Item1.Item1, periods.Item1.Item2);
                Run(periods.Item2.Item1, periods.Item2.Item2);
                Algo.DayStart();
            }
            if (AutoClosePositions) Algo.ClosePositions(Algo.Symbol, null);
            Algo.Stop();
        }
    }


    public class Optimizer<T> where T : AlgoBase
    {
        public string FileName { get; set; }

        public OptimizerSettings Settings { get; set; }

        Type algoType;

        public Optimizer(OptimizerSettings settings, Type algoType)
        {
            this.Settings = settings;
            this.algoType = algoType;
            RandomGenerator random = new RandomGenerator();
            this.FileName = $"c:\\kalitte\\log\\simulation\\results\\br-{Settings.Start.ToString("yyyy-MM-dd")}-{Settings.Finish.ToString("yyyy-MM-dd")}-{(random.Next(1000000, 9999999))}.tsv";
        }

        private Backtest run(Dictionary<string, object> init, int index, int total, string[] configs, Backtest related = null)
        {
            var algo = (AlgoBase)Activator.CreateInstance(typeof(T), new Object[] { init });
            algo.SimulationFile = this.FileName;
            algo.SimulationFileFields = configs;
            algo.MultipleTestOptimization = total > 1;
            Backtest test = new Backtest(algo, Settings.Start, Settings.Finish, related);
            test.AutoClosePositions = Settings.AutoClosePositions;
            //Console.WriteLine($"Running test case {i}/{cases.Count} for {algo.InstanceName} using {algo.LogFile}");
            try
            {
                test.Start();
                Console.WriteLine($"Completed case {algo.InstanceName}[{index}/{total}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in instance {algo.InstanceName}. {ex}");
            }
            return test;
        }

        public void clearAlternates()
        {

        }

        public void Start()
        {
            var alternates = Settings.Alternates;
            Console.WriteLine("Generating test cases ...");
            var allCase = alternates.GenerateTestCases(Settings.SamplingPerc == 0);
            List<Dictionary<string, object>> cases;
            if (Settings.SamplingPerc == 0) cases = allCase;
            else
            {
                cases = new List<Dictionary<string, object>>();
                int total = allCase.Count * Settings.SamplingPerc / 100;
                var random = new RandomGenerator();
                Console.WriteLine($"Sampling {total} items ...");
                for (int i = 0; i < total; i++)
                {
                    var r = random.Next(0, allCase.Count);
                    cases.Add(allCase[r]);
                }
            }
            Console.WriteLine($" ** WILL RUN {cases.Count}/{allCase.Count} TESTS ** Hit to continue ...");
            var headers = CreateHeaders(this.FileName);
            Console.WriteLine($"Running tests to file {this.FileName}");
            var completed = 0;
            //Backtest related = run(cases[0], ++completed, cases.Count, headers);            
            Parallel.For(0, cases.Count, i =>
            {
                var initValues = cases[i];
                run(initValues, ++completed, cases.Count, headers);
            });
            Console.WriteLine(" ** COMPLETED ** Hit to close ...");
            //Console.ReadKey();
        }

        private string[] CreateHeaders(string resultFile)
        {
            var multiple = Settings.Alternates.Where(p => p.Value.Length > 1);
            var dictionary = multiple.Any() ? multiple.Select(p => p.Key).ToArray() : AlgoBase.GetConfigValues(typeof(T)).Select(p => p.Key).ToArray();
            var sb = new StringBuilder();

            //F_XU0300222: long/ 1 / Cost: 2250.75 Total: 2250.75 PL: -32.25 Commission: 39.15 NetPL: -71.40
            sb.Append("Pos\tQuantity\tCost\tTotal\tPL\tCommission\tNetPL\tOrdertotal\tLog\t");
            foreach (var key in dictionary) sb.Append(key + "\t");
            sb.Append(Environment.NewLine);
            File.WriteAllText(resultFile, sb.ToString());
            return dictionary;
        }
    }
}
