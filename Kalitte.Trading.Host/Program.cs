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
        settings.Finish = new DateTime(2022, 3, 16);
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30));
        var alternates = settings.Alternates = new AlternateValues(initValues);

        // options
        alternates.Set("CrossOrderQuantity", 9);
        alternates.Set("RsiTrendOrderQuantity", 9);
        alternates.Set("DataCollectSize", 8);
        alternates.Set("DataAnalysisSize", 80);
        alternates.Set("DataCollectUseSma", false);
        alternates.Set("DataAnalysisUseSma", true);
        alternates.Set("UsePerformanceMonitor", false);


        // profit && loss
        alternates.Set("ProfitInitialQuantity", 4);
        alternates.Set("ProfitKeepQuantity", 1,2);
        alternates.Set("ProfitQuantityStep", 2,1);
        alternates.Set("ProfitQuantityStepMultiplier", 0);
        alternates.Set("ProfitStart", 10);        
        alternates.Set("ProfitPriceStep", 3.0,2);
        alternates.Set("ProfitUseMonitor", false);

        // rsiTrendProfit
        alternates.Set("RsiProfitInitialQuantity", 0);
        alternates.Set("RsiProfitKeepQuantity", 1);
        alternates.Set("RsiProfitStart", 12);
        alternates.Set("RsiProfitPriceStep", 4.0);
        alternates.Set("RsiProfitEnableLimitingSignalsOnStart", false);

        alternates.Set("PriceLowLimit", 2200);
        alternates.Set("PriceHighLimit", 2300);                   

        alternates.Set("LossInitialQuantity", 9);
        alternates.Set("LossKeepQuantity", 0);
        alternates.Set("LossQuantityStep", 0);
        alternates.Set("LossQuantityStepMultiplier", 0);                
        alternates.Set("LossStart", 288);
        alternates.Set("LossPriceStep", 0);

        // rsi
        alternates.Set("RsiHighLimit", 73);
        alternates.Set("RsiLowLimit", 32);
        alternates.Set("RsiProfitDeltaHighLimit", 3.0);
        alternates.Set("RsiProfitDeltaLowLimit", 7.0);
        alternates.Set("Rsi", 14);
        alternates.Set("RsiValueSignalSensitivity", 3M);        

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

        alternates.Set("CrossRsiMax",  55.6);
        alternates.Set("CrossRsiMin",  45.2);        
        alternates.Set("PowerCrossThreshold", 88);
        alternates.Set("PowerCrossNegativeMultiplier", 1.3);
        alternates.Set("PowerCrossPositiveMultiplier", 2.8);

        // general
        alternates.Set("ClosePositionsDaily", false);     
        
        alternates.Set("RsiGradientTolerance", 0.015);
        alternates.Set("RsiGradientLearnRate", 0.005);

        alternates.Set("ProfitGradientTolerance", 0.001);
        alternates.Set("ProfitGradientLearnRate", 0.002);

        alternates.Set("RsiGradientSensitivity", 1);
                

        // System
        alternates.Set("LoggingLevel", LogLevel.Verbose);        
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("LogConsole", false);
        alternates.SaveToFile($"c:\\kalitte\\Bist30-test.json");

        alternates.Set("LoggingLevel", LogLevel.Order);
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

                var initValues = AlgoBase.GetConfigValues(typeof(Bist30));
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
        
        var optimize = new Optimizer<Bist30>(settings, typeof(Bist30));
        optimize.Start();
    }
}



