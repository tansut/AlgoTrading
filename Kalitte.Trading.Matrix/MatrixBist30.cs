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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Kalitte.Trading.Matrix
{




    public class MatrixBist30 : MatrixAlgoBase<Bist30>
    {

        public override void OnInit()
        {
            AddSymbol(Algo.Symbol, (SymbolPeriod)Enum.Parse(typeof(SymbolPeriod), Algo.SymbolPeriod.ToString()));
            //AddSymbol(Symbol, SymbolPeriod.Min);
            WorkWithPermanentSignal(true);
            SendOrderSequential(false);
            AddSymbolMarketData(Algo.Symbol);
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







        protected override Bist30 createAlgoInstance()
        {
            var fileName = $"c:\\kalitte\\{this.GetType().Name}.json";
            Bist30 algo;
            Dictionary<string, object> init = null;

            var file = File.ReadAllText(fileName);
            var fileContent = JsonConvert.DeserializeObject<Dictionary<string, object[]>>(file);
            init = new AlternateValues(fileContent).Lean();
            init["Symbol"] = "F_XU0300422";
            algo = new Bist30(init);
            //var content = new AlternateValues(algo.GetConfigValues());
            //File.WriteAllText(fileName, JsonConvert.SerializeObject(content, Formatting.Indented));
            return algo;
        }
    }
}

