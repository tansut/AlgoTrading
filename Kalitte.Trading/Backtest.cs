using Kalitte.Trading.Algos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class Backtest
    {
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public AlgoBase Algo { get; set; }

        public Backtest(AlgoBase algo, DateTime start, DateTime end)
        {
            Algo = algo;
            StartTime = start;
            FinishTime = end;
        }

        public void Start()
        {
            Algo.Simulation = true;
            Algo.UseVirtualOrders = true;
            Algo.Symbol = "F_XU0300222";
            Algo.SymbolPeriod = BarPeriod.Min10;

            Algo.AlgoTime = StartTime;
            var seconds = Algo.GetSymbolPeriodSeconds(Algo.SymbolPeriod.ToString());

            Algo.Init();

            var lastDay = new DateTime(Algo.AlgoTime.Year, Algo.AlgoTime.Month, Algo.AlgoTime.Day).AddDays(-1).AddHours(22).AddMinutes(50);

            Algo.LoadBars(lastDay);
            Algo.InitMySignals(Algo.AlgoTime);
            Algo.InitCompleted();



            //if (simulationCount > 12) return;                                       






            for (var p = StartTime; p < FinishTime;)
            {
               
                Algo.Log($"Running backtest for period: {Algo.PeriodBars.Last}", LogLevel.Verbose);

                for (var i = 0; i < seconds; i++)
                {
                    var time = Algo.AlgoTime;
                    foreach (var signal in Algo.Signals)
                    {
                        var result = signal.Check(time);
                        //var waitOthers = waitForOperationAndOrders("Backtest");
                    }
                    Algo.CheckDelayedOrders(time);
                    Algo.AlgoTime = Algo.AlgoTime.AddSeconds(1);
                }
                Algo.simulationCount++;

                
                var bd = Algo.GetPeriodBars(p).Last;
                var newQuote = new MyQuote() { Date = p, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                Algo.PeriodBars.Push(newQuote);
                Algo.Log($"Pushed new bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Verbose);

                p = p.AddSeconds(seconds);
                Algo.AlgoTime = p;
            }


            Algo.Stop();
        }

    }
}
