// algo
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using Matriks.Data.Symbol;
using Matriks.Indicators;
using Matriks.Lean.Algotrader.Models;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Trader.Core.TraderModels;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading.Matrix
{




    public class MaProfit : MatrixAlgoBase<MyAlgo>
    {

        //[SymbolParameter("F_XU0300222")]
        public string Symbol { get; set; } = "F_XU0300222";


        [Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod { get; set; } = SymbolPeriod.Min10;

        [Parameter(4)]
        public decimal OrderQuantity { get; set; } = 2M;


        [Parameter(2)]
        public int CrossPriceCollectionPeriod { get; set; } = 2;


        [Parameter(4)]
        public int RsiPriceCollectionPeriod { get; set; } = 4;

        [Parameter(true)]
        public bool UseSmaForCross { get; set; } = true;

        [Parameter(5)]
        public int MovPeriod { get; set; } = 5;

        [Parameter(9)]
        public int MovPeriod2 { get; set; } = 9;


        [Parameter(0.25)]
        public decimal MaAvgChange { get; set; } = 0.25M;

        [Parameter(25)]
        public int MaPeriods { get; set; } = 25;

        [Parameter(true)]
        public bool DynamicCross { get; set; } = true;


        [Parameter(0)]
        public decimal ExpectedNetPl { get; set; } = 0;




        [Parameter(false)]
        public bool UseVirtualOrders { get; set; } = false;

        [Parameter(false)]
        public bool AutoCompleteOrders { get; set; } = false;

        [Parameter(false)]
        public bool SimulateOrderSignal { get; set; } = false;

        [Parameter(1)]
        public decimal ProfitQuantity { get; set; } = 1;

        [Parameter(0)]
        public decimal LossQuantity { get; set; } = 0;

        [Parameter(16)]
        public decimal ProfitPuan { get; set; } = 16;

        [Parameter(4)]
        public decimal LossPuan { get; set; } = 4;

        [Parameter(60.0)]
        public decimal RsiHighLimit { get; set; } = 60M;

        [Parameter(40.0)]
        public decimal RsiLowLimit { get; set; } = 40M;

        [Parameter(2)]
        public decimal MinRsiChange { get; set; } = 2M;

        [Parameter(1)]
        public decimal RsiProfitQuantity { get; set; } = 1M;

        [Parameter(1)]
        public decimal RsiProfitPuan { get; set; } = 1M;

        [Parameter(14)]
        public int Rsi { get; set; } = 14;

        [Parameter(60)]
        public int RsiAnalysisPeriod { get; set; } = 60;

        [Parameter(0)]
        public int MACDShortPeriod { get; set; } = 0;

        [Parameter(9)]
        public int MACDLongPeriod { get; set; } = 9;

        [Parameter(0.05)]
        public decimal MacdAvgChange { get; set; } = 0.05M;

        [Parameter(15)]
        public int MacdPeriods { get; set; } = 15;

        [Parameter(9)]
        public int MACDTrigger { get; set; } = 9;

        [Parameter(false)]
        public bool AlwaysGetProfit { get; set; } = false;

        [Parameter(false)]
        public bool AlwaysStopLoss { get; set; } = false;

        [Parameter(60)]
        public int PowerLookback { get; set; } = 60;

        [Parameter(60)]
        public int PowerBarSeconds { get; set; } = 60;

        [Parameter(15)]
        public int PowerVolumeCollectionPeriod { get; set; } = 15;

        private DateTime? lastSimulationDay = null;


        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            if ((ProfitQuantity > 0 || LossQuantity > 0) && !Simulation)
            {
                AddSymbolMarketData(Symbol);
            }
            SetAlgoProperties();
            base.OnInit();
        }



        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            if (Simulation)
            {
                lock (this)
                {                    
                    var bd = barDataCurrentValues.LastUpdate;
                    Algo.AlgoTime = bd.DTime;
                    var algoDay = new DateTime(Algo.AlgoTime.Year, Algo.AlgoTime.Month, Algo.AlgoTime.Day);

                    var seconds = Algo.GetSymbolPeriodSeconds(SymbolPeriod.ToString());

                    //if (simulationCount > 12) return;                                       

                    var lastDay = algoDay.AddDays(-1).AddHours(22).AddMinutes(50);
                    if (Algo.SignalsState != StartableState.Started)
                    {                        
                        Algo.InitializeBars(this.Symbol, Algo.SymbolPeriod, lastDay);
                        Algo.InitMySignals(lastDay);
                        Algo.InitCompleted();
                    }

                    if (!lastSimulationDay.HasValue) lastSimulationDay = algoDay;

                    if ((algoDay - lastSimulationDay.Value).Days > 0)
                    {
                        lastSimulationDay = Algo.AlgoTime;
                        Algo.Signals.ForEach(p => p.Reset());
                    }

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

                    //for (var i = 0; i < seconds; i++)
                    //{
                    //    var time = Algo.AlgoTime;
                    //    foreach (var signal in Algo.Signals)
                    //    {
                    //        var result = signal.Check(time);
                    //    }
                    //    Algo.CheckDelayedOrders(time);
                    //    Algo.AlgoTime = Algo.AlgoTime.AddSeconds(1);
                    //}
                    Algo.simulationCount++;
                    var newQuote = new MyQuote() { Date = barDataCurrentValues.LastUpdate.DTime, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                    Algo.PushNewBar(Symbol, Algo.SymbolPeriod, newQuote.Date, newQuote);
                }
            }
            else
            {                
                var bd = GetBarData(Symbol, SymbolPeriod);
                var last = bd.BarDataIndexer.LastBarIndex;
                try
                {
                    var newQuote = new MyQuote() { Date = bd.BarDataIndexer[last], High = bd.High[last], Close = bd.Close[last], Low = bd.Low[last], Open = bd.Open[last], Volume = bd.Volume[last] };
                    Algo.PushNewBar(Symbol, (BarPeriod)Enum.Parse(typeof(BarPeriod), this.SymbolPeriod.ToString()), newQuote.Date, newQuote);
                }
                catch (Exception ex)
                {
                    Algo.Log($"data update: {ex.Message}", LogLevel.Error);
                }
            }
        }



        public override void OnInitCompleted()
        {           
            if (!Simulation)
            {
                var assembly = typeof(MaProfit).Assembly.GetName();
                Algo.Log($"{this}", LogLevel.Info);
                if (Algo.UseVirtualOrders)
                {
                    Algo.Log($"Using ---- VIRTUAL ORDERS ----", LogLevel.Warning);
                }
                LoadRealPositions(Algo.Symbol);
                Algo.InitializeBars(this.Symbol, Algo.SymbolPeriod, DateTime.Now);
                Algo.InitMySignals(DateTime.Now);
                Algo.InitCompleted();
            }
        }




        protected override MyAlgo createAlgoInstance()
        {
            return new MyAlgo();
        }
    }

}

