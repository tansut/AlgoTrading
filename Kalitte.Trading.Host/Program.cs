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
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

public class Settings
{
    public DateTime sDate { get; set; }
    public DateTime fDate { get; set; }
    public AlternateValues Alternates { get; set; }
}

public class Program
{

    public static Settings LoadFromFile(string fileName)
    {
        var file = File.ReadAllText(fileName);
        var obj = JsonConvert.DeserializeObject<Settings>(file);
        return obj;
    }

    public static void SaveToFile(string fileName, Settings values)
    {
        var json = JsonConvert.SerializeObject(values, new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented

        });
        File.WriteAllText(fileName, json);
    }

    public static Settings AppTest()
    {
        var settings = new Settings();
        settings.sDate = new DateTime(2022, 02, 25, 9, 30, 0);
        settings.fDate = new DateTime(2022, 02, 25, 23, 0, 0);

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
        var alternates = settings.Alternates = new AlternateValues(initValues);
                
        // options
        alternates.Set("OrderQuantity", 6);
        

        // profit && loss
        alternates.Set("ProfitQuantity", 0);
        alternates.Set("ProfitPuan", 16);
        alternates.Set("LossQuantity", 0);
        alternates.Set("LossPuan", 0);
        alternates.Set("RsiProfitQuantity", 1);
        alternates.Set("RsiProfitPuan", 2);
        alternates.Set("ProgressiveProfitLoss", 1.75);
        
        // rsi
        alternates.Set("RsiHighLimit", 0);
        alternates.Set("RsiLowLimit", 0);
        alternates.Set("Rsi", 14);
        alternates.Set("MinRsiChange", 1M);


        // volume power
        alternates.Set("PowerVolumeCollectionPeriod", 30);
        alternates.Set("PowerLookback", 5);


        // ma cross
        alternates.Set("DynamicCross", true);
        alternates.Set("MaAvgChange", 0.25M);
        alternates.Set("MaPeriods", 45);
        alternates.Set("CrossPriceCollectionPeriod", 30);
        alternates.Set("PowerCrossThreshold", 90);
        alternates.Set("PowerCrossNegativeMultiplier", 1);
        alternates.Set("PowerCrossPositiveMultiplier", 2.5);

        // general
        alternates.Set("ClosePositionsDaily", true);

        
        // System
        alternates.Set("LoggingLevel", LogLevel.Order);
        alternates.Set("Symbol", "F_XU0300222");
        alternates.Set("LogConsole", true);

        var file = $"c:\\kalitte\\Bist30Futures-test.json";
        var val = JsonConvert.SerializeObject(alternates, Formatting.Indented);
        File.WriteAllText(file, val);
        return settings;
    }

    public static void Main(string[] args)
    {

        Settings settings = new Settings();

        if (args.Length == 0)
        {
            settings = AppTest();
        }
        else if (args.Length == 1)
        {
            settings = LoadFromFile(args[0]);
        }
        else if (args.Length == 2)
        {
            if (args[1] == "init")
            {

                var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
                settings.Alternates = settings.Alternates = new AlternateValues(initValues);
                SaveToFile(args[0], settings);
                return;
            }
            else if (args[1] == "backtest")
            {
                settings = LoadFromFile(args[0]);
                var firstValues = settings.Alternates.Lean();
                settings.Alternates = new AlternateValues(firstValues);
            } else
            {
                Console.WriteLine("Unknown option");
                return;
            }

        }
        var optimize = new Optimizer<Bist30Futures>(settings.sDate, settings.fDate, typeof(Bist30Futures));
        optimize.Start(settings.Alternates);
    }

}

