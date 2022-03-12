// algo
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using Matriks.Data.Symbol;
using Matriks.Indicators;
using Matriks.Lean.Algotrader.Models;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Trader.Core.TraderModels;
using Newtonsoft.Json;
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




    public class MaProfit : MatrixAlgoBase<Bist30Futures>
    {

        //[SymbolParameter("F_XU0300222")]
        //public string Symbol { get; set; } = "F_XU0300222";


        //// [Parameter(SymbolPeriod.Min10)]
        //public SymbolPeriod SymbolPeriod { get; set; } = SymbolPeriod.Min10;

        //// [Parameter(2)]
        //public decimal OrderQuantity { get; set; } 


        //// [Parameter(10)]
        //public int CrossPriceCollectionPeriod { get; set; } 


        //// [Parameter(4)]
        //public int RsiPriceCollectionPeriod { get; set; } 

        //// [Parameter(true)]
        //public bool UseSmaForCross { get; set; } 

        //// [Parameter(5)]
        //public int MovPeriod { get; set; } 

        //// [Parameter(9)]
        //public int MovPeriod2 { get; set; } 


        //// [Parameter(0.25)]
        //public decimal MaAvgChange { get; set; } 

        //// [Parameter(60)]
        //public int MaPeriods { get; set; } 

        //// [Parameter(false)]
        //public bool DynamicCross { get; set; } 


        //// [Parameter(0)]
        //public decimal ExpectedNetPl { get; set; }




        //// [Parameter(false)]
        //public bool UseVirtualOrders { get; set; } 

        //// [Parameter(false)]
        //public bool AutoCompleteOrders { get; set; } 

        //// [Parameter(false)]
        //public bool SimulateOrderSignal { get; set; } 

        //// [Parameter(1)]
        //public decimal ProfitQuantity { get; set; }

        //// [Parameter(0)]
        //public decimal LossQuantity { get; set; } 

        ///// [Parameter(16)]
        //public decimal ProfitPuan { get; set; } 

        //// [Parameter(4)]
        //public decimal LossPuan { get; set; }

        //Parameter(60.0)]
        //public decimal RsiHighLimit { get; set; } 

        // [Parameter(40.0)]
        //public decimal RsiLowLimit { get; set; } 

        // [Parameter(2)]
        //public decimal MinRsiChange { get; set; } 

        // [Parameter(1)]
        //public decimal RsiProfitQuantity { get; set; } 

        // [Parameter(1)]
        //public decimal RsiProfitPuan { get; set; } 

        // [Parameter(14)]
        //public int Rsi { get; set; }

        // [Parameter(60)]
        //public int RsiAnalysisPeriod { get; set; } 



        // [Parameter(0)]
        //public int MACDShortPeriod { get; set; } 

        // [Parameter(9)]
        //public int MACDLongPeriod { get; set; } 

        // [Parameter(0.05)]
        //public decimal MacdAvgChange { get; set; } 

        // [Parameter(15)]
        //public int MacdPeriods { get; set; } 

        // [Parameter(9)]
        //public int MACDTrigger { get; set; } 

        // [Parameter(false)]
        //public bool AlwaysGetProfit { get; set; } 

        // [Parameter(false)]
        //public bool AlwaysStopLoss { get; set; } 

        // [Parameter(5)]
        //public int PowerLookback { get; set; }

        // [Parameter(100)]
        //public decimal PowerCrossThreshold { get; set; }

        // [Parameter(10)]
        //public int PowerVolumeCollectionPeriod { get; set; } 

        //private DateTime? lastSimulationDay = null;


        public override void OnInit()
        {
            AddSymbol(Algo.Symbol, (SymbolPeriod)Enum.Parse(typeof(SymbolPeriod), Algo.SymbolPeriod.ToString()));
            //AddSymbol(Symbol, SymbolPeriod.Min);
            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            AddSymbolMarketData(Algo.Symbol);
            SetAlgoProperties();
            base.OnInit();
        }



        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            var period = barDataCurrentValues.LastUpdate.SymbolPeriod;
            var bd = GetBarData(Algo.Symbol, period);
            var last = bd.BarDataIndexer.LastBarIndex;
            try
            {
                var newQuote = new MyQuote() { Date = bd.BarDataIndexer[last], High = bd.High[last], Close = bd.Close[last], Low = bd.Low[last], Open = bd.Open[last], Volume = bd.Volume[last] };
                Algo.PushNewBar(Algo.Symbol, (BarPeriod)Enum.Parse(typeof(BarPeriod), period.ToString()), newQuote);
            }
            catch (Exception ex)
            {
                Algo.Log($"data update: {ex.Message}", LogLevel.Error);
            }
        }







        protected override Bist30Futures createAlgoInstance()
        {
            var fileName = @"c:\kalitte\Bist30Futures.json";
            Bist30Futures algo;
            Dictionary<string, object> init = null ;
            if (File.Exists(fileName))
            {
                var file = File.ReadAllText(fileName);
                var fileContent = JsonConvert.DeserializeObject<Dictionary<string, object [] >>(file);
                init = new AlternateValues(fileContent).Lean();
            }
            init["Symbol"] = "F_XU0300422";
            algo = new Bist30Futures(init);             
            var content =new AlternateValues(algo.GetConfigValues());
            File.WriteAllText(fileName, JsonConvert.SerializeObject(content, Formatting.Indented));
            return algo;
        }
    }
}

