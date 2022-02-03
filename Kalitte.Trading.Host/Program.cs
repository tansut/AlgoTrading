using Kalitte.Trading;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {


        var s = new List<string>("5,4,6,8,12,14,16,18,20".Split(',')).Select(x => decimal.Parse(x)).ToArray();

        TopQue q = new TopQue(s.Length);


        List<Quote> quotes = new List<Quote>();
        foreach (var n in s)
        {
            quotes.Add(new Quote() { Close = n, Date = DateTime.Now });
            q.Push(n);
        };

        for (var i = 1; i < s.Length + 1; i++)
        {
            //Console.WriteLine("lib");
            //Console.WriteLine(String.Join(",", quotes.GetEma(i).Select(p => p.Ema).ToArray()));
            //Console.WriteLine(String.Join(",", quotes.GetSma(i).Select(p => p.Sma).ToArray()));
            Console.WriteLine("me");

            Console.WriteLine(q.CalcEma(i).Last());


        }


        Console.WriteLine(s.Length);
        Console.WriteLine(q.Average());
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

        for (var i = 0; i < 100 ; i++)
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
}

