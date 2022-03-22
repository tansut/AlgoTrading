// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kalitte.Trading.Algos;

namespace Kalitte.Trading.Matrix
{
    public class BarDataLogger : MatrixAlgoBase<Log>
    {

        [Parameter(1)]
        public int LogSeconds = 1;

        [SymbolParameter("F_XU0300422")]
        public string Symbol = "F_XU0300422";

        //[Parameter(SymbolPeriod.Min10)]
        //public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

        
        private MarketDataFileLogger priceLogger;
        VOLUME volume;

        List<SymbolPeriod> periodList = new List<SymbolPeriod>(new SymbolPeriod[] { SymbolPeriod.Min, SymbolPeriod.Min5, SymbolPeriod.Min10, SymbolPeriod.Min15,
            SymbolPeriod.Min20, SymbolPeriod.Min30, SymbolPeriod.Min60, SymbolPeriod.Min120, SymbolPeriod.Min180, SymbolPeriod.Min240
        });

        List<IIndicator> indicatorList = new List<IIndicator>();
        List<MarketDataFileLogger> loggerList = new List<MarketDataFileLogger>();


        public override void OnTimer()
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);

            volume.RefreshIndicator = true;            

            if (t >= t1 && t <= t2)
            {
                lock(this)
                {                    
                    var price = GetMarketData(Symbol, SymbolUpdateField.Last);
                    priceLogger.LogMarketData(DateTime.Now, new decimal[] { price, volume.CurrentValue });
                }
            }
        }

        public override void OnInitCompleted()
        {

            List<ISymbolBarData> barDataList = new List<ISymbolBarData>();
            try
            {
                foreach (var sp in periodList)
                {
                    var fbd = GetBarData(Symbol, sp);
                    for (var i = 0; i < fbd.BarDataIndexer.LastBarIndex; i++)
                    {
                        var bdidx = periodList.FindIndex(p => p.ToString() == fbd.PeriodInfo.ToSymbolPeriod().ToString());
                        var logger = loggerList[bdidx];
                        if (!logger.GetMarketData(fbd.BarDataIndexer[i]).HasValue)
                            LogBardata(fbd, i);

                        //Debug($"{i} {fbd.BarDataIndexer[i].ToString()}  o:{fbd.Open[i]} h:{fbd.High[i]} l:{fbd.Low[i]} c:{fbd.Close[i]} wc:{fbd.WClose[i]} dif:{fbd.Diff[i]} dif%:{fbd.DiffPercent[i]} vol:{fbd.Volume[i]}");


                    }
                }
            } catch(Exception ex)
            {
                Debug($"{ex.Message}");
            }
        }



        public override void OnInit()
        {           
            AddSymbolMarketData(Symbol);
            SetTimerInterval(1);
            WorkWithPermanentSignal(true);

            this.priceLogger = new MarketDataFileLogger(Symbol, Algo.LogDir, "price");

            foreach (var sp in periodList)
            {
                AddSymbol(Symbol, sp);
                var logger = new MarketDataFileLogger(Symbol, Algo.LogDir, "" + sp.ToString());
                logger.SaveDaily = true;
                logger.FileName = "all.txt";
                loggerList.Add(logger);
            }

            volume = VolumeIndicator(Symbol, SymbolPeriod.Min10);
            volume.RefreshIndicator = true;
        }

        void LogBardata(ISymbolBarData bd, int i)
        {
            var bdidx = periodList.FindIndex(p => p.ToString() == bd.PeriodInfo.ToSymbolPeriod().ToString());
            loggerList[bdidx].LogMarketData(bd.BarDataIndexer[i], new decimal[] { 
                bd.Open[i], 
                bd.High[i], 
                bd.Low[i], 
                bd.Close[i], 
                bd.WClose[i], 
                bd.Volume[i], 
                bd.Diff[i], 
                bd.DiffPercent[i]});
        }

        public override void OnDataUpdate(BarDataEventArgs barDataEventArgs)
        {
            try
            {
               
                var bdidx = periodList.FindIndex(p => p == barDataEventArgs.PeriodInfo.ToSymbolPeriod());

                var bd = GetBarData(Symbol, periodList[bdidx]);
                var logger = loggerList[bdidx];
                var i = bd.BarDataIndexer.LastBarIndex;
                LogBardata(bd, i);

            } catch(Exception ex)
            {
                Algo.Log($"{ex.Message} / {ex.StackTrace}");
            }
           
        }

        protected override Log createAlgoInstance()
        {
            return new Log();
        }
    }

}
