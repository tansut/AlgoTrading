using Kalitte.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;

public class Program
{
    static HubConnection connection;


    public static void Main()
    {

        var b1 = new FinanceBars(5);



        


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
            var nextt = macd.Trigger.NextValue(list[i].Close);
            var nextm= macd.NextValue(list[i].Close);
            Console.WriteLine(list[i].Date.ToString());
            Console.WriteLine(list[i].Close.ToString());
            Console.WriteLine(nextt);
            Console.WriteLine(nextm);
            bars.Push(list[i]);
            //var testd = testBars.Ema(5).Find(p => p.Date == ema5.InputBars.Last.Date);
            //var alld = allBars.Ema(5).Find(p => p.Date == ema5.InputBars.Last.Date);
           Console.WriteLine($"EMA5: { ema5.InputBars.Last.Date}  {ema5.Results.Last.Ema}");
            //Console.WriteLine($"TestEMA5: { testd.Date}  {testd.Ema}");
            //Console.WriteLine($"allema5: { alld.Date}  {alld.Ema}");
            //Console.WriteLine($"RES: { testd.Ema != ema5.ResultBars.Last.Close}  ");
            //Console.WriteLine($"RES2: { alld.Ema != ema5.ResultBars.Last.Close}  ");
            //Console.WriteLine($"EMA9: { ema9.InputBars.Last.Date}  {ema9.ResultBars.Last.Close}");
            Console.WriteLine($"MACD: { macd.InputBars.Last.Date}  {macd.Results.Last.Macd}");
            Console.WriteLine($"MACD: { macd.InputBars.Last.Date}  {macd.Trigger.Results.Last.Ema}");

            //Console.WriteLine($"MACDT: { macd.InputBars.Last.Date}  {macd.Trigger.ResultBars.Last.Close}");
        }


        //for (var i = 0; i < ema5.Bars.List.Length; i++)
        //{
        //    Console.WriteLine($"EMA5: { ema5.Bars.List[i].Date}  {em5list[i].Ema}");
        //    Console.WriteLine($"EMA9: { ema9.Bars.List[i].Date}  {em9list[i].Ema}");
        //}

        //Console.WriteLine(ema5.LastValue(2500));



        //Console.WriteLine($"{ em5list }");
        //Console.WriteLine($"{ em5list }");

        Console.ReadLine();

 


        //var list = string.Join(",", bars.List.Select(p => p.Close.ToString()));
        //Console.WriteLine(list);
        ////bars.Push(new Quote(85));

        // list = string.Join(",", bars.List.Select(p => p.Close.ToString()));
        //Console.WriteLine(list);


        Console.WriteLine(bars.List.GetEma(9).Last());
        Console.WriteLine(bars.Cross(6));
        Console.WriteLine(bars.Cross(12));
        Console.WriteLine(bars.Cross(13));
        Console.WriteLine(bars.Cross(20));
        Console.WriteLine(bars.Cross(50));


        Console.ReadLine();

        return;

        //connection = new HubConnection("https://localhost:44392");
        
        //    connection.Headers.Add("headername", "headervalue");
        //    IHubProxy stockTickerHubProxy = connection.CreateHubProxy("pricefeed");
        //    stockTickerHubProxy.On<string, decimal>("feed", (symbol, price) =>
        //    {
        //        Console.WriteLine("message: " + symbol + "-" + price);
        //    });
        //connection.Start(new LongPollingTransport()).Wait();

        //    Console.WriteLine("connect done");

        //    Console.ReadLine();

        //stockTickerHubProxy.Invoke("feed", "fff", 12, DateTime.Now);

        

        //    Console.ReadLine();

        //    // connection.Send("denee123").Wait();


        







        ////connection. <string, string>("ReceiveMessage", (user, message) =>
        ////{
        ////    Console.WriteLine(user + message);

        ////});

        ////try
        ////{
        ////     connection.Start().Wait();
        ////}
        ////catch (Exception ex)
        ////{
        ////    Console.WriteLine(ex.Message);
        ////}

        //Console.ReadLine();


        var s = new List<string>("5,4,6,8,12,14,16,18,20".Split(',')).Select(x => decimal.Parse(x)).ToArray();

        TopQue q = new TopQue(s.Length);
        FinanceBars qbar = new FinanceBars(s.Length);

        List<Quote> quotes = new List<Quote>();
        foreach (var n in s)
        {
            //quotes.Add(new Quote() { Close = n, Date = DateTime.Now });
            q.Push(n);
            qbar.Push(new Quote() { Close = n, Date = DateTime.Now });
        };

        for (var i = 1; i < s.Length + 1; i++)
        {
            //Console.WriteLine("lib");
            //Console.WriteLine(String.Join(",", quotes.GetEma(i).Select(p => p.Ema).ToArray()));
            //Console.WriteLine(String.Join(",", quotes.GetSma(i).Select(p => p.Sma).ToArray()));
            Console.WriteLine("me");

            Console.WriteLine(q.CalcEma(i).Last());
            Console.WriteLine(qbar.List.GetEma(i).Last());


        }


        Console.WriteLine(s.Length);
        //Console.WriteLine(q.Average());
        Console.WriteLine(q.ExponentialMovingAverage);

        //Console.ReadLine();

        //q.Push(2200);
        //q.Push(2205);
        //q.Push(2204);
        //q.Push(2210);
        //q.Push(2215);
        //Console.WriteLine(q.Average);
        //Console.WriteLine(q.ExponentialMovingAverage);
        //Console.ReadLine();


        MarketDataFileLogger logger = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte", "ma59");
        logger.LogMarketData(DateTime.Now, new decimal[] { 12, 78, 90 });
        logger.Seperator = ',';
        var content = logger.GetContentValues(@"c:\kalitte\history-10min.csv");
        int period = 50000;
        TopQue q1 = new TopQue(period);

        for (var i = 0; i < content.Count; i++)
        {
            q1.Add(content[i][3]);
        }

        for (var i = 0; i < 100; i++)
        {
            Console.WriteLine($"{i} {content[i][3]} {q1.CalcEma(5)[i]} {q1.CalcEma(9)[i]}");
        }

        //TopQue q2 = new TopQue(period);

        //CrossCalculator cc = new CrossCalculator();
        //for (var i= content.Count-1; i>=0; i--)
        //{
        //    q1.Push(content.Values[i][1]);
        //    q2.Push(content.Values[i][2]);
        //    if (q2.Count >= period)
        //    {
        //        bool? result = null;
        //        if (cc.CrossAboveX(q1, q2, 0.25M))
        //        {
        //            result = true;
        //        } else if (cc.CrossBelowX(q1, q2, 0.25M))
        //        {
        //            result = false;
        //        }
        //        if (result.HasValue)
        //        {
        //            Console.WriteLine($"{(result.Value ? 'A' : 'B')} d: {content.Keys[i]} p: {content.Values[i][0]}");
        //        }
        //    }
        //}
        Console.WriteLine("done");
        Console.ReadLine();
        //var t = DateTime.Now;
        //logger.LogMarketData(t, new decimal[] { 256, 1477 });

        //var val = logger.GetMarketData();
        //var val2 = logger.GetMarketDataList(t);
        //Console.WriteLine(val.ToString());
        //Console.WriteLine(val2);
    }

    private static void Connection_Closed()
    {
        throw new NotImplementedException();
    }
}

