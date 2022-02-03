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
    public class PriceLogger : AlgoBase
    {



        [Parameter(1)]
        public int LogSeconds = 1;

        [SymbolParameter("F_XU0300222")]
        public string Symbol = "F_XU0300222";


        public string logDir = @"c:\kalitte\log";
        private MarketDataFileLogger priceLogger;
        private MarketDataFileLogger rsiLogger;
        private MarketDataFileLogger ma59Logger;
        private MarketDataFileLogger macd953Logger;

        MOV mov;
        MOV mov2;
        RSI rsi;
        MACD macd;
        VOLUME volume;

        SymbolPeriod SymbolPeriod = SymbolPeriod.Min10;

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

            mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod, MovMethod.Exponential);
            mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, MovPeriod2, MovMethod.Exponential);
            rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
            macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, MACDLongPeriod, MACDShortPeriod, MACDTrigger);
            volume = VolumeIndicator(Symbol, SymbolPeriod);


            this.priceLogger = new MarketDataFileLogger(Symbol, logDir, "price");
            this.rsiLogger = new MarketDataFileLogger(Symbol, logDir, "rsi");
            this.ma59Logger = new MarketDataFileLogger(Symbol, logDir, "ma59");
            //this.ma9Logger = new MarketDataFileLogger(Symbol, logDir, "ma9");
            this.macd953Logger = new MarketDataFileLogger(Symbol, logDir, "macd953");
        }

        public override void OnTimer()
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);
            
            
            if (t >= t1 && t <= t2)
            {
                var price = GetMarketData(Symbol, SymbolUpdateField.Last);
                priceLogger.LogMarketData(DateTime.Now, new decimal[] { price, volume.CurrentValue });
                rsiLogger.LogMarketData(DateTime.Now, new decimal[] { price, rsi.CurrentValue });
                ma59Logger.LogMarketData(DateTime.Now, new decimal[] { price, mov.CurrentValue, mov2.CurrentValue, CrossBelow(mov, mov2) ? 1 : 0, CrossAbove(mov, mov2) ? 1 : 0 });
                macd953Logger.LogMarketData(DateTime.Now, new decimal[] { price, macd.CurrentValue, macd.MacdTrigger.CurrentValue, CrossBelow(macd, macd.MacdTrigger) ? 1 : 0, CrossAbove(macd, macd.MacdTrigger) ? 1 : 0 });
            }
        }
    }

}
