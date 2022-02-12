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

namespace Kalitte.Trading.Matrix
{
    public class BarDataLogger : AlgoBase
    {



        [Parameter(1)]
        public int LogSeconds = 1;

        [SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";

        [Parameter(SymbolPeriod.Min10)]
        public SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;


        public string logDir = @"c:\kalitte\log";
        //private MarketDataFileLogger logger;
        //private MarketDataFileLogger rsiLogger;
        //private MarketDataFileLogger ma59Logger;
        //private MarketDataFileLogger macd953Logger;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;
        VOLUME volume;

        List<SymbolPeriod> periodList = new List<SymbolPeriod>(new SymbolPeriod[] { SymbolPeriod.Min, SymbolPeriod.Min5, SymbolPeriod.Min10, SymbolPeriod.Min15,
            SymbolPeriod.Min20, SymbolPeriod.Min30, SymbolPeriod.Min60, SymbolPeriod.Min120, SymbolPeriod.Min180, SymbolPeriod.Min240
        });

        List<IIndicator> indicatorList = new List<IIndicator>();
        List<MarketDataFileLogger> loggerList = new List<MarketDataFileLogger>();

        int MovPeriod = 5;
        int MovPeriod2 = 9;

        int MACDLongPeriod = 9;
        int MACDShortPeriod = 5;

        int MACDTrigger = 3;



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
                        var bdidx = periodList.FindIndex(p => p == fbd.PeriodInfo.ToSymbolPeriod());
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

            foreach (var sp in periodList)
            {
                AddSymbol(Symbol, sp);
                var logger = new MarketDataFileLogger(Symbol, logDir, "" + sp.ToString());
                logger.SaveDaily = true;
                logger.FileName = "all.txt";
                loggerList.Add(logger);
            }

            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);
            volume = VolumeIndicator(Symbol, SymbolPeriod);


        }

        void LogBardata(ISymbolBarData bd, int i)
        {
            var bdidx = periodList.FindIndex(p => p == bd.PeriodInfo.ToSymbolPeriod());
            loggerList[bdidx].LogMarketData(bd.BarDataIndexer[i], new decimal[] { 
                bd.Open[i], 
                bd.High[i], 
                bd.Low[i], 
                bd.Close[i], 
                bd.WClose[i], 
                bd.Volume[i], 
                bd.Diff[i], 
                bd.DiffPercent[i], 
                mov.CurrentValue, 
                mov2.CurrentValue, 
                rsi.CurrentValue, 
                macd.CurrentValue, 
                macd.MacdTrigger.CurrentValue });


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
                Log($"{ex.Message} / {ex.StackTrace}");
            }
           
        }
    }

}
