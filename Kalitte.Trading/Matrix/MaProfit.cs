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

        [SymbolParameter("F_XU0300222")]
        public string Symbol { get; set; } = "F_XU0300222";


        [Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod { get; set; } = SymbolPeriod.Min10;

        [Parameter(2)]
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


        [Parameter(0.15)]
        public decimal MaAvgChange { get; set; } = 0.15M;

        [Parameter(15)]
        public int MaPeriods { get; set; } = 1;


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

        [Parameter(3)]
        public decimal MinRsiChange { get; set; } = 3M;

        [Parameter(9)]
        public int Rsi { get; set; } = 9;

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
                        Algo.LoadBars(this.Symbol, lastDay);
                        Algo.InitMySignals(lastDay);
                        Algo.InitCompleted();
                    }

                    if (!lastSimulationDay.HasValue) lastSimulationDay = algoDay;

                    if ((algoDay - lastSimulationDay.Value).Days > 0)
                    {
                        lastSimulationDay = Algo.AlgoTime;
                        //var bd2 = Algo.GetPeriodBars(Algo.Symbol, lastDay).Last;
                        //var dayQuote = new MyQuote() { Date = lastDay, High = bd2.High, Close = bd2.Close, Low = bd2.Low, Open = bd2.Open, Volume = bd2.Volume };
                        //Algo.PeriodBars.Push(dayQuote);
                        //Algo.Log($"Pushed new day bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Debug);
                        Algo.Signals.ForEach(p => p.Reset());
                    }

                    Algo.Log($"Running backtest for period: {Algo.PeriodBars.Last}", LogLevel.Debug);

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

                    var newQuote = new MyQuote() { Date = barDataCurrentValues.LastUpdate.DTime, High = bd.High, Close = bd.Close, Low = bd.Low, Open = bd.Open, Volume = bd.Volume };
                    Algo.PeriodBars.Push(newQuote);
                    Algo.Log($"Pushed new bar, last bar is now: {Algo.PeriodBars.Last}", LogLevel.Debug);
                }
            }
            else
            {
                var bd = GetBarData(Symbol, SymbolPeriod);
                var last = bd.BarDataIndexer.LastBarIndex;
                try
                {
                    Algo.WaitSignalOperations();
                    var newQuote = new MyQuote() { Date = bd.BarDataIndexer[last], High = bd.High[last], Close = bd.Close[last], Low = bd.Low[last], Open = bd.Open[last], Volume = bd.Volume[last] };
                    Algo.PeriodBars.Push(newQuote);
                    Algo.Log($"Pushed new quote, last is now: {Algo.PeriodBars.Last}", LogLevel.Debug);

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
                Algo.LoadBars(this.Symbol, DateTime.Now);
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

