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



public class Program
{

    public static OptimizerSettings LoadFromFile(string fileName)
    {
        var file = File.ReadAllText(fileName);
        var obj = JsonConvert.DeserializeObject<OptimizerSettings>(file);
        return obj;
    }

    public static void SaveToFile(string fileName, OptimizerSettings values)
    {
        var json = JsonConvert.SerializeObject(values, new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented

        });
        File.WriteAllText(fileName, json);
    }

    public static OptimizerSettings AppTest()
    {
        var settings = new OptimizerSettings();
        settings.Start = new DateTime(2022, 2, 28);
        settings.Finish = new DateTime(2022, 3, 1);
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
        var alternates = settings.Alternates = new AlternateValues(initValues);

        // options
        alternates.Set("OrderQuantity", 8);
        alternates.Set("DataCollectSize", 12);
        alternates.Set("DataAnalysisSize", 48);
        alternates.Set("DataCollectUseSma", false);
        alternates.Set("DataAnalysisUseSma", true);


        // profit && loss
        alternates.Set("ProfitInitialQuantity", 3);
        alternates.Set("ProfitKeepQuantity", 1);
        alternates.Set("ProfitQuantityStep", 1);
        alternates.Set("ProfitQuantityStepMultiplier", 0);
        alternates.Set("ProfitStart", 10.0);        
        alternates.Set("ProfitPriceStep", 3.0);

        alternates.Set("LossInitialQuantity", 0);
        alternates.Set("LossKeepQuantity", 3);
        alternates.Set("LossQuantityStep", 1);
        alternates.Set("LossQuantityStepMultiplier", 2);                
        alternates.Set("LossStart", 12);
        alternates.Set("LossPriceStep", 4);

        alternates.Set("RsiProfitInitialQuantity", 3);
        alternates.Set("RsiProfitKeepQuantity", 1);
        alternates.Set("RsiProfitQuantityStep", 1);
        alternates.Set("RsiProfitQuantityStepMultiplier", 0);
        alternates.Set("RsiProfitStart", 10.0);
        alternates.Set("RsiProfitPriceStep", 3.0);


        
        


        // rsi
        alternates.Set("RsiHighLimit", 60);
        alternates.Set("RsiLowLimit", 40);
        alternates.Set("Rsi", 14);        
        alternates.Set("RsiTrendSensitivity", 1.5M);
        alternates.Set("RsiTrendThreshold", 1M);

        // price

        alternates.Set("PriceTrendSensitivity", 2.5M);


        // volume power
        alternates.Set("PowerLookback", 5);

        // ma cross        
        alternates.Set("MaAvgChange", 0.32M);
        alternates.Set("DynamicCross", true);
        alternates.Set("PowerCrossThreshold", 88);
        alternates.Set("PowerCrossNegativeMultiplier", 1.3);
        alternates.Set("PowerCrossPositiveMultiplier", 2.8);

        // general
        alternates.Set("ClosePositionsDaily", false);

        
        // System
        alternates.Set("LoggingLevel", LogLevel.Warning);
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("LogConsole", true);

        var file = $"c:\\kalitte\\Bist30Futures-test.json";
        var val = JsonConvert.SerializeObject(alternates, Formatting.Indented);
        File.WriteAllText(file, val);
        SaveToFile("c:\\kalitte\\lastrun.json", settings);
        return settings;
    }

    public static void Main(string[] args)
    {

        OptimizerSettings settings = new OptimizerSettings();

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
        clearAlternates(settings.Alternates);
        var optimize = new Optimizer<Bist30Futures>(settings, typeof(Bist30Futures));
        optimize.Start();
    }

    public static void clearAlternates(AlternateValues values)
    {
        
    }

}

