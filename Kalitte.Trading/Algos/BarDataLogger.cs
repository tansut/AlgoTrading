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

namespace Kalitte.Trading.Algos
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
        private MarketDataFileLogger logger;
        //private MarketDataFileLogger rsiLogger;
        //private MarketDataFileLogger ma59Logger;
        //private MarketDataFileLogger macd953Logger;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;
        VOLUME volume;

        

        int MovPeriod = 5;
        int MovPeriod2 = 9;

        int MACDLongPeriod = 9;
        int MACDShortPeriod = 5;

        int MACDTrigger = 3;


        public override void OnInit()
        {
            AddSymbol(Symbol, SymbolPeriod);
            AddSymbolMarketData(Symbol);
            SetTimerInterval(1);
            WorkWithPermanentSignal(true);

            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);
            volume = VolumeIndicator(Symbol, SymbolPeriod);

            this.logger = new MarketDataFileLogger(Symbol, logDir, "" + SymbolPeriod.ToString());
            logger.SaveDaily = true;
        }

        public override void OnDataUpdate(BarDataEventArgs barDataEventArgs)
        {
            var fbd = GetBarData(Symbol, SymbolPeriod);

            Debug($"--1t:  o:{fbd.Open} h:{fbd.High} l:{fbd.Low} c:{fbd.Close} wc:{fbd.WClose} dif:{fbd.Diff} dif%:{fbd.DiffPercent} vol:{fbd.Volume}");

            var fbd2 = barDataEventArgs.BarData;

            Debug($"--2t: {fbd2.Dtime} bt: {fbd2.BarType}   o:{fbd2.Open} h:{fbd2.High} l:{fbd2.Low} c:{fbd2.Close} wc:{fbd2.WClose} dif:{fbd.Diff} dif%:{fbd.DiffPercent} vol:{fbd.Volume}");

        }

        public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
        {
            var bd = barDataCurrentValues.LastUpdate;
            

            //logger.LogMarketData(bd.DTime, new decimal[] { bd.Open, bd.High, bd.Low, bd.Close, bd.WClose, bd.LastQuantity, bd.Volume, bd.Diff, bd.DiffPercent, mov.CurrentValue, mov2.CurrentValue, rsi.CurrentValue, macd.CurrentValue, macd.MacdTrigger.CurrentValue });

            var fbd = GetBarData(Symbol, SymbolPeriod);

            Debug($"*1t:   o:{fbd.Open} h:{fbd.High} l:{fbd.Low} c:{fbd.Close} wc:{fbd.WClose} dif:{fbd.Diff} dif%:{fbd.DiffPercent} vol:{fbd.Volume}");




            //list.Add($"t: {bd.DTime} o:{bd.Open} h:{bd.High} l:{bd.Low} c:{bd.Close} wc:{bd.WClose} dif:{bd.Diff} dif%:{bd.DiffPercent} vol:{bd.Volume}");
            //list.Add($"t: {bd.DTime} rsi: {rsi.CurrentValue} ma5: {mov.CurrentValue} ma9: {mov2.CurrentValue} macd: {macd.CurrentValue} {macd.MacdTrigger.CurrentValue}");
            //list.Add($"t: {bd.DTime} macb: {CrossBelow(mov, mov2)} maca: {CrossAbove(mov, mov2)} macdcb: {CrossBelow(macd, macd.MacdTrigger)} macdcb: {CrossAbove(macd, macd.MacdTrigger)}");


            //priceLogger.LogMarketData(DateTime.Now, new decimal[] { price, volume.CurrentValue });
            //rsiLogger.LogMarketData(DateTime.Now, new decimal[] { price, rsi.CurrentValue });
            //ma59Logger.LogMarketData(DateTime.Now, new decimal[] { price, mov.CurrentValue, mov2.CurrentValue, CrossBelow(mov, mov2) ? 1 : 0, CrossAbove(mov, mov2) ? 1 : 0 });
            //macd953Logger.LogMarketData(DateTime.Now, new decimal[] { price, macd.CurrentValue, macd.MacdTrigger.CurrentValue, CrossBelow(macd, macd.MacdTrigger) ? 1 : 0, CrossAbove(macd, macd.MacdTrigger) ? 1 : 0 });

        }

    }

}
