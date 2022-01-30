using Kalitte.Trading;
using System;

public class Program
{
    public static void Main()
    {
        MarketDataFileLogger logger = new MarketDataFileLogger("F_XU0300222", @"c:\kalitte\log");
        var val = logger.GetMarketPrice(new DateTime(2022, 01, 27, 17, 30, 12));
        Console.WriteLine(val.ToString());
    }
}

