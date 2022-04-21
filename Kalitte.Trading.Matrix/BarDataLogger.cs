// algo
using Kalitte.Trading.Algos;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Lean.Algotrader.Models;
using Matriks.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kalitte.Trading.Matrix
{
    public class LogInfo
    {
        public string Symbol { get; set; }

        public int SymbolId { get; set; }
        public MarketDataFileLogger PriceLogger { get; set; }
        public List<VOLUME> Volumes { get; set; }

        public List<MarketDataFileLogger> BarLoggers { get; set; }
        

    }

    public class BarDataLogger : MatrixAlgoBase<Log>
    {

        List<string> SymbolList = new List<string>(new[] { "F_XU0300422", "F_XAUUSD0422", "F_XU0300622", "F_XAUUSD0622", "F_XAGUSD0422", "F_XAGUSD0622", "F_EURUSD0422", "F_EURUSD0622" });

        public List<LogInfo> Logs { get; set; } = new List<LogInfo> {  };

        List<SymbolPeriod> periodList = new List<SymbolPeriod>(new SymbolPeriod[] { SymbolPeriod.Min, SymbolPeriod.Min5, SymbolPeriod.Min10, SymbolPeriod.Min15,
            SymbolPeriod.Min20, SymbolPeriod.Min30, SymbolPeriod.Min60, SymbolPeriod.Min120, SymbolPeriod.Min180, SymbolPeriod.Min240
        });
        
       


        public override void OnTimer()
        {
            var t = DateTime.Now;
            var t1 = new DateTime(t.Year, t.Month, t.Day, 9, 30, 0);
            var t2 = new DateTime(t.Year, t.Month, t.Day, 23, 0, 0);

            if (t >= t1 && t <= t2 && t.DayOfWeek != System.DayOfWeek.Saturday && t.DayOfWeek != System.DayOfWeek.Sunday)
            {
                lock (this)
                {
                    foreach (var symbol in SymbolList)
                    {
                        var log = Logs.Find(p=>p.Symbol == symbol);
                        log.Volumes.ForEach(volume => volume.RefreshIndicator = true);
                        var price = GetMarketData(symbol, SymbolUpdateField.Last);
                        
                        var data = log.Volumes.Select(p=>p.CurrentValue).ToList();
                        data.Insert(0, price);
                        log.PriceLogger.LogMarketData(DateTime.Now, data.ToArray());
                    }
                }
            }
        }

        public override void OnInitCompleted()
        {

            List<ISymbolBarData> barDataList = new List<ISymbolBarData>();
            try
            {
                foreach (var symbol in SymbolList)
                {
                    var log = Logs.Find(p=>p.Symbol == symbol);
                    foreach (var sp in periodList)
                    {
                        var fbd = GetBarData(symbol, sp);
                        for (var i = 0; i < fbd.BarDataIndexer.LastBarIndex; i++)
                        {
                            var bdidx = periodList.FindIndex(p => p.ToString() == fbd.PeriodInfo.ToSymbolPeriod().ToString());
                            var logger = log.BarLoggers[bdidx];
                            if (!logger.GetMarketData(fbd.BarDataIndexer[i]).HasValue)
                                LogBardata(log, fbd, i);

                            //Debug($"{i} {fbd.BarDataIndexer[i].ToString()}  o:{fbd.Open[i]} h:{fbd.High[i]} l:{fbd.Low[i]} c:{fbd.Close[i]} wc:{fbd.WClose[i]} dif:{fbd.Diff[i]} dif%:{fbd.DiffPercent[i]} vol:{fbd.Volume[i]}");


                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug($"{ex.Message}");
            }
        }



        public override void OnInit()
        {            
            SetTimerInterval(1);
            WorkWithPermanentSignal(true);

            foreach (var symbol in SymbolList)
            {
                AddSymbolMarketData(symbol);
                
                var priceLogger = new MarketDataFileLogger(symbol, Algo.LogDir, "price");

                var log = new LogInfo();
                Logs.Add(log);
                log.Symbol = symbol;
                log.SymbolId = GetSymbolId(symbol);
                log.PriceLogger = priceLogger;

                var volumes = new List<VOLUME>();

                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min10));
                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min15));
                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min20));
                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min30));
                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min60));
                volumes.Add(VolumeIndicator(symbol, SymbolPeriod.Min120));

                log.Volumes = volumes;
                log.BarLoggers = new List<MarketDataFileLogger>();



                foreach (var sp in periodList)
                {
                    AddSymbol(symbol, sp);

                    var logger = new MarketDataFileLogger(symbol, Algo.LogDir, "" + sp.ToString());
                    logger.SaveDaily = true;
                    logger.FileName = "all.txt";
                    log.BarLoggers.Add(logger);
                }
            }
            


            




        }

        void LogBardata(LogInfo log, ISymbolBarData bd, int i)
        {
            
            var bdidx = periodList.FindIndex(p => p.ToString() == bd.PeriodInfo.ToSymbolPeriod().ToString());
            log.BarLoggers[bdidx].LogMarketData(bd.BarDataIndexer[i], new decimal[] {
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
                var log = Logs.Find(p=>p.SymbolId == barDataEventArgs.SymbolId);
                var bdidx = periodList.FindIndex(p => p == barDataEventArgs.PeriodInfo.ToSymbolPeriod());

                var bd = GetBarData(log.Symbol, periodList[bdidx]);
                var logger = log.BarLoggers[bdidx];
                var i = bd.BarDataIndexer.LastBarIndex;
                LogBardata(log, bd, i);

            }
            catch (Exception ex)
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
