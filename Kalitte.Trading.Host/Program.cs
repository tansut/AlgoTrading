using Kalitte.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using Kalitte.Trading.Tests;
using Kalitte.Trading.Matrix;
using Kalitte.Trading.Algos;

public class Program
{



    public static void Main()
    {


        var algo = new MyAlgo();

        var sDate = new DateTime(2022,02,17, 9,30,0);
        var fDate = new DateTime(2022,02,18, 23,0,0);

        var initValues = new Dictionary<string, object>();
        initValues.Add("ProfitPuan", 16M);

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

