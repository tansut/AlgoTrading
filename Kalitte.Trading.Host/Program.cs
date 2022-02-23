using Kalitte.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using Kalitte.Trading.Matrix;
using Kalitte.Trading.Algos;

public class Program
{



    public static void Main()
    {


        var algo = new MyAlgo();

        var sDate = new DateTime(2022,02,22, 9,30,0);
        var fDate = new DateTime(2022,02,22, 23,0,0);

        var initValues = new Dictionary<string, object>();



        //initValues.Add("LossQuantity", 1M);
        //initValues.Add("LossPuan", 8M);
        //initValues.Add("Rsi", 9);
        //initValues.Add("MinRsiChange", 2M);
        //initValues.Add("RsiProfitPuan", 0.1M);
        //initValues.Add("RsiHighLimit", 0M);
        //initValues.Add("RsiLowLimit", 0M);
        //initValues.Add("RsiProfitPuan", 2M);
        //initValues.Add("ProfitPuan", 16M);
        //initValues.Add("ProfitQuantity", 2M);
        //initValues.Add("OrderQuantity", 2M);
        //initValues.Add("RsiProfitQuantity", 1M);
        initValues.Add("DynamicCross", true);
        //initValues.Add("MaAvgChange", 0.25M);
        initValues.Add("MaPeriods", 60);
        initValues.Add("CrossPriceCollectionPeriod", 10);
        //initValues.Add("PowerLookback", 60);
        //initValues.Add("PowerBarSeconds", 60);
        initValues.Add("PowerVolumeCollectionPeriod", 10);
        initValues.Add("PowerCrossThreshold", 50);

        //initValues.Add("UseSmaForCross", false);











        //initValues.Add("CrossPriceCollectionPeriod", 2);
        //initValues.Add("MaAvgChange", 0.15M);
        //initValues.Add("MaPeriods", 15);
        //initValues.Add("RsiAnalysisPeriod", 50);
        //initValues.Add("RsiPriceCollectionPeriod", 4);

        initValues.Add("LoggingLevel", LogLevel.Warning);

        Backtest t = new Backtest(algo, sDate, fDate, initValues);
        t.Start();

        return;

        //var test = new TestAlgo();

        

        var b1 = new FinanceBars(5);


        var l = new List<MyQuote>();
        l.Add(new MyQuote() { Date = DateTime.Now, Close=25 });
        l.Add(new MyQuote() { Date = DateTime.Now, Close=75 });

        Console.Write(l.GetSma(2).Last().Sma.Value);



        var mdp = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "Min10");
        mdp.SaveDaily = true;
        mdp.FileName = "history.txt";
        var bars = mdp.GetContentAsQuote(DateTime.Now);

        var mdp2 = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "Min10");
        mdp2.SaveDaily = true;
        mdp2.FileName = "new.txt";

        var mdp3 = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "Min10");
        mdp3.SaveDaily = true;
        mdp3.FileName = "test.txt";

        var mdp4 = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "Min10");
        mdp4.SaveDaily = true;
        mdp4.FileName = "all.txt";

        var testBars = mdp3.GetContentAsQuote(DateTime.Now);
        var newBars = mdp2.GetContentAsQuote(DateTime.Now);
        var allBars = mdp4.GetContentAsQuote(DateTime.Now);

        var ema5 = new Ema(bars, 5);
        //var ema9 = new Ema(bars, 9);
        var macd = new Macd(bars,  5,9, 3);

        //var nextt = macd.Trigger.NextValue(2422);

        //Console.WriteLine(nextt);


        //var em5list = bars.Ema(5);// string.Join(",", bars.Ema(5).Select(p => p.ToString()));
        //var em9list = bars.Ema(9);// string.Join(",", bars.Ema(9).Select(p => p.ToString()));

        var list = newBars.List;

        for (var i = 0; i < newBars.Count; i++)
        {
            //var nextt = macd.Trigger.NextValue(list[i].Close);
            //var nextm= macd.NextValue(list[i].Close);
            //Console.WriteLine(list[i].Date.ToString());
            //Console.WriteLine(list[i].Close.ToString());
            //Console.WriteLine(nextt);
            //Console.WriteLine(nextm);
            //bars.Push(list[i]);
            //var testd = testBars.Ema(5).Find(p => p.Date == ema5.InputBars.Last.Date);
            //var alld = allBars.Ema(5).Find(p => p.Date == ema5.InputBars.Last.Date);
           Console.WriteLine($"EMA5: { ema5.InputBars.Last.Date}  {ema5.ResultList.Last.Ema}");
            //Console.WriteLine($"TestEMA5: { testd.Date}  {testd.Ema}");
            //Console.WriteLine($"allema5: { alld.Date}  {alld.Ema}");
            //Console.WriteLine($"RES: { testd.Ema != ema5.ResultBars.Last.Close}  ");
            //Console.WriteLine($"RES2: { alld.Ema != ema5.ResultBars.Last.Close}  ");
            //Console.WriteLine($"EMA9: { ema9.InputBars.Last.Date}  {ema9.ResultBars.Last.Close}");
            Console.WriteLine($"MACD: { macd.InputBars.Last.Date}  {macd.ResultList.Last.Macd}");
            Console.WriteLine($"MACD: { macd.InputBars.Last.Date}  {macd.Trigger.ResultList.Last.Ema}");

            //Console.WriteLine($"MACDT: { macd.InputBars.Last.Date}  {macd.Trigger.ResultBars.Last.Close}");
        }


    }

}

