using Kalitte.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Kalitte.Trading.Indicators;

public class Program
{
    static HubConnection connection;


    public static void Main()
    {
        var mdp = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "Min10");
        mdp.SaveDaily = true;
        var bars = mdp.GetContentAsQuote(DateTime.Now);
        var ema5 = new Ema(bars, 5);
        var ema9 = new Ema(bars, 9);


        var em5list = bars.Ema(5);// string.Join(",", bars.Ema(5).Select(p => p.ToString()));
        var em9list = bars.Ema(9);// string.Join(",", bars.Ema(9).Select(p => p.ToString()));


        for (var i = 0; i < ema5.Bars.List.Length; i++)
        {
            Console.WriteLine($"{ ema5.Bars.List[i].Date} { ema5.Bars.List[i].Close} {em5list[i]}");
        }

        Console.WriteLine(ema5.LastValue(2500));



        Console.WriteLine($"{ em5list }");
        Console.WriteLine($"{ em5list }");

        Console.ReadLine();

        bars = new Bars(5);
        bars.Push(new Quote(DateTime.Now, 12));
        bars.Push(new Quote(DateTime.Now, 15));
        bars.Push(new Quote(DateTime.Now, 20));
        bars.Push(new Quote(DateTime.Now, 30));
        bars.Push(new Quote(DateTime.Now, 16));


        var list = string.Join(",", bars.List.Select(p => p.Close.ToString()));
        Console.WriteLine(list);
        //bars.Push(new Quote(85));

         list = string.Join(",", bars.List.Select(p => p.Close.ToString()));
        Console.WriteLine(list);


        Console.WriteLine(bars.Ema().Last());
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
        Bars qbar = new Bars(s.Length);

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
            Console.WriteLine(qbar.Ema(i).Last());


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

