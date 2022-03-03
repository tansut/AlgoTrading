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
        settings.Start = new DateTime(2022, 2, 25);
        settings.Finish = new DateTime(2022, 2, 25);
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
        alternates.Set("ProfitStart", 10.0);        
        alternates.Set("ProfitPriceStep", 2.0);

        alternates.Set("LossInitialQuantity", 0);
        alternates.Set("LossKeepQuantity", 0);
        alternates.Set("LossQuantityStep", 1);
        alternates.Set("LossQuantityStepMultiplier", 0);                
        alternates.Set("LossStart", 16);
        alternates.Set("LossPriceStep", 4);


        // rsi profit
        alternates.Set("RsiProfitInitialQuantity", 1);
        alternates.Set("RsiProfitKeepQuantity", 0);
        alternates.Set("RsiProfitQuantityStep", 1);
        alternates.Set("RsiProfitQuantityStepMultiplier", 0);
        alternates.Set("RsiProfitStart", 6.0);
        alternates.Set("RsiProfitPriceStep", 1.0);

        alternates.Set("RsiLossStart", 4.0);

        // rsi
        alternates.Set("RsiHighLimit", 69);
        alternates.Set("RsiLowLimit", 31);
        alternates.Set("Rsi", 14);        
        alternates.Set("RsiTrendSensitivity", 2M);
        alternates.Set("RsiTrendThreshold", 0.1);

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

