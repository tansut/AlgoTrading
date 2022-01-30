using Kalitte.Trading;
using System;
using System.Collections.Generic;

public class Program
{
    public static void Main()
    {
        MarketDataFileLogger logger = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log", "price");
        var t = DateTime.Now;
        logger.LogMarketData(t, new decimal[] { 256, 1477 });

        var val = logger.GetMarketData(new DateTime(2022, 01, 28, 17, 30, 12));
        var val2 = logger.GetMarketDataList(t);
        Console.WriteLine(val.ToString());
        Console.WriteLine(val2);
    }
}

