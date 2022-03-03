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


    public static OptimizerSettings AppTest()
    {
        var settings = new OptimizerSettings();
        settings.Start = new DateTime(2022, 2, 28);
        settings.Finish = new DateTime(2022, 3, 2);
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
        var alternates = settings.Alternates = new AlternateValues(initValues);

        // options
        alternates.Set("CrossOrderQuantity", 6);
        alternates.Set("RsiTrendOrderQuantity", 0);
        alternates.Set("DataCollectSize", 12);
        alternates.Set("DataAnalysisSize", 48);
        alternates.Set("DataCollectUseSma", false);
        alternates.Set("DataAnalysisUseSma", true);


        // profit && loss
        alternates.Set("ProfitInitialQuantity", 3);
        alternates.Set("ProfitKeepQuantity", 1);
        alternates.Set("ProfitQuantityStep", 1);
        alternates.Set("ProfitQuantityStepMultiplier", 0);
        alternates.Set("ProfitStart", 12.0);        
        alternates.Set("ProfitPriceStep", 4.0);

        alternates.Set("LossInitialQuantity", 0);
        alternates.Set("LossKeepQuantity", 0);
        alternates.Set("LossQuantityStep", 1);
        alternates.Set("LossQuantityStepMultiplier", 0);                
        alternates.Set("LossStart", 16);
        alternates.Set("LossPriceStep", 4);


        // rsi profit
        alternates.Set("RsiProfitInitialQuantity", 3);
        alternates.Set("RsiProfitKeepQuantity", 1);
        alternates.Set("RsiProfitQuantityStep", 1);
        alternates.Set("RsiProfitQuantityStepMultiplier", 0);
        alternates.Set("RsiProfitStart", 9.0);
        alternates.Set("RsiLossStart", 8);
        alternates.Set("RsiProfitPriceStep", 1.0);

        // rsi
        alternates.Set("RsiHighLimit", 60);
        alternates.Set("RsiLowLimit", 40);
        alternates.Set("Rsi", 14);        
        alternates.Set("RsiTrendSensitivity", 2M);
        alternates.Set("RsiTrendThreshold", 0.6, 0.65, 0.7,0.8,0.85,0.9);

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
        alternates.Set("LoggingLevel", LogLevel.Debug);        
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("LogConsole", true);

        alternates.SaveToFile($"c:\\kalitte\\Bist30Futures-test.json");
        alternates.Set("LoggingLevel", LogLevel.Warning);

        settings.SaveToFile("c:\\kalitte\\lastrun.json");
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
            settings = OptimizerSettings.LoadFromFile(args[0]);
        }
        else if (args.Length == 2)
        {
            if (args[1] == "init")
            {

                var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
                settings.Alternates = settings.Alternates = new AlternateValues(initValues);
                settings.SaveToFile(args[0]);
                return;
            }
            else if (args[1] == "backtest")
            {
                settings = OptimizerSettings.LoadFromFile(args[0]);
                var firstValues = settings.Alternates.Lean();
                settings.Alternates = new AlternateValues(firstValues);
            } else
            {
                Console.WriteLine("Unknown option");
                return;
            }

        }
        
        var optimize = new Optimizer<Bist30Futures>(settings, typeof(Bist30Futures));
        optimize.Start();
    }

}

