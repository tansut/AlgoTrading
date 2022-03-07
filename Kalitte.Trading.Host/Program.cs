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
        settings.Finish = new DateTime(2022, 3, 7);
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30Futures));
        var alternates = settings.Alternates = new AlternateValues(initValues);

        // options
        alternates.Set("CrossOrderQuantity", 6);
        alternates.Set("RsiTrendOrderQuantity", 2);
        alternates.Set("DataCollectSize", 8);
        alternates.Set("DataAnalysisSize", 48);
        alternates.Set("DataCollectUseSma", false);
        alternates.Set("DataAnalysisUseSma", true);


        // profit && loss
        alternates.Set("ProfitInitialQuantity", 3);
        alternates.Set("ProfitKeepQuantity", 1);
        alternates.Set("ProfitQuantityStep", 1);
        alternates.Set("ProfitQuantityStepMultiplier", 0);
        alternates.Set("ProfitStart", 9);        
        alternates.Set("ProfitPriceStep", 2.0);

        alternates.Set("PriceLowLimit", 2200);
        alternates.Set("PriceHighLimit", 2300);                   

        alternates.Set("LossInitialQuantity", 2);
        alternates.Set("LossKeepQuantity", 1);
        alternates.Set("LossQuantityStep", 1);
        alternates.Set("LossQuantityStepMultiplier", 0);                
        alternates.Set("LossStart", 50);
        alternates.Set("LossPriceStep", 250);


        // rsi profit
        alternates.Set("RsiProfitInitialQuantity",  0);
        alternates.Set("RsiProfitKeepQuantity", 0);
        alternates.Set("RsiProfitQuantityStep", 1);
        alternates.Set("RsiProfitQuantityStepMultiplier", 0);
        alternates.Set("RsiProfitStart", 6);
        alternates.Set("RsiProfitPriceStep", 1.0);

        alternates.Set("RsiLossStart", 5);

        // rsi
        alternates.Set("RsiHighLimit", 69);
        alternates.Set("RsiLowLimit", 31);
        alternates.Set("Rsi", 14);        
        alternates.Set("RsiTrendSensitivity", 3M);
        alternates.Set("RsiTrendThreshold", 0.1);

        // price
        alternates.Set("PriceTrendSensitivity", 2.5M);

        alternates.Set("MaTrendSensitivity", 7M);
        alternates.Set("MaTrendThreshold", 0.1);

        // volume power
        alternates.Set("PowerLookback", 5);

        // ma cross        
        alternates.Set("MaAvgChange", 0.32M);
        alternates.Set("MovPeriod", 5);
        alternates.Set("MovPeriod2", 9);
        alternates.Set("DynamicCross", true);

        // macd
        alternates.Set("MacdAvgChange", 0.32M);
        alternates.Set("MACDShortPeriod", 0);
        alternates.Set("MACDLongPeriod", 9);
        alternates.Set("MACDTrigger", 9);



        alternates.Set("UseVolumeWeightMa", false);
        alternates.Set("CrossRsiMax",  56);
        alternates.Set("CrossRsiMin",  44);        
        alternates.Set("PowerCrossThreshold", 88);
        alternates.Set("PowerCrossNegativeMultiplier", 1.3);
        alternates.Set("PowerCrossPositiveMultiplier", 2.8);

        // general
        alternates.Set("ClosePositionsDaily", false);


        // System
        alternates.Set("LoggingLevel", LogLevel.Verbose);        
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("LogConsole", false);

        alternates.SaveToFile($"c:\\kalitte\\Bist30Futures-test.json");
        alternates.Set("LoggingLevel", LogLevel.Warning);
        alternates.Set("LogConsole", true);
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

