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
        settings.Start = new DateTime(2022, 3, 16);
        settings.Finish = new DateTime(2022, 3, 16);
        settings.AutoClosePositions = true;
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30));
        var alternates = settings.Alternates = new AlternateValues(initValues);

        // options
        alternates.Set("CrossOrderQuantity", 9);

        alternates.Set("RsiOrderL1/Quantity", 1);
        alternates.Set("RsiOrderL1/Action", RsiPositionAction.OpenIfEmpty);

        alternates.Set("RsiOrderL2/Quantity", 3);
        alternates.Set("RsiOrderL2/Action", RsiPositionAction.BuyAdditional);

        alternates.Set("RsiOrderL3/Quantity", 4);
        alternates.Set("RsiOrderL3/Action", RsiPositionAction.ChangePosition);



        alternates.Set("UsePerformanceMonitor", false);

        // analyser defaults
        alternates.Set("DataCollectSize", 8);
        alternates.Set("DataAnalysisSize", 80);
        alternates.Set("DataCollectAverage", Average.Ema);
        alternates.Set("DataAnalysisAverage", Average.Sma);
        
        // profit && loss
        alternates.Set("Profit/InitialQuantity", 3,5,4);
        alternates.Set("Profit/KeepQuantity", 2,1,3);
        alternates.Set("Profit/QuantityStep", 1,2);
        alternates.Set("Profit/QuantityStepMultiplier", 0);
        alternates.Set("Profit/Start", 10,9);        
        alternates.Set("Profit/PriceStep", 1,2.0);
        alternates.Set("Profit/PriceMonitor", false);

        // fibonachi
        alternates.Set("PriceLowLimit", 2400);
        alternates.Set("PriceHighLimit", 2500);                   

        alternates.Set("RsiLoss/Enabled", true);
        alternates.Set("RsiLoss/InitialQuantity", 9);
        alternates.Set("RsiLoss/KeepQuantity", 0);
        alternates.Set("RsiLoss/QuantityStep", 0);
        alternates.Set("RsiLoss/QuantityStepMultiplier", 0);                
        alternates.Set("RsiLoss/Start", 96);
        alternates.Set("RsiLoss/PriceStep", 0);


        // rsi
        alternates.Set("Rsi", 14);

        alternates.Set("RsiHighL1/L1", 73.00);        
        alternates.Set("RsiHighL1/L2", 76.00);

        alternates.Set("RsiHighL2/L1", 76.01);
        alternates.Set("RsiHighL2/L2", 83.00);

        alternates.Set("RsiHighL3/L1", 83.01);
        alternates.Set("RsiHighL3/L2", 100);

        alternates.Set("RsiLowL1/L1", 33.00);
        alternates.Set("RsiLowL1/L2", 29.00);

        alternates.Set("RsiLowL2/L1", 28.99);
        alternates.Set("RsiLowL2/L2", 23.00);

        alternates.Set("RsiLowL3/L1", 22.99);
        alternates.Set("RsiLowL3/L2", 0);

        alternates.Set("RsiGradientTolerance", 0.02);
        alternates.Set("RsiGradientLearnRate", 0.005);

        // volume power signal
        alternates.Set("PowerLookback", 5);

        // ma cross        
        alternates.Set("MaCross/AvgChange", 0.32M);
        alternates.Set("MovPeriod", 5);
        alternates.Set("MovPeriod2", 9);
        alternates.Set("MaCross/Dynamic", true);
        alternates.Set("MaCross/PowerThreshold", 88);
        alternates.Set("MaCross/PowerNegativeMultiplier", 1.3);
        alternates.Set("MaCross/PowerPositiveMultiplier", 2.8);

        // cross rsi
        alternates.Set("CrossRsiMax",  55.6);
        alternates.Set("CrossRsiMin",  45.2);
        alternates.Set("RsiValue/SignalSensitivity", 4M);


        // general
        alternates.Set("DailyClose/Enabled", true);     
        alternates.Set("DailyClose/KeepQuantity", 2);

        // System
        alternates.Set("LoggingLevel", LogLevel.Verbose);        
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("LogConsole", false);
        alternates.SaveToFile($"c:\\kalitte\\Bist30-test.json");    
        alternates.Set("LoggingLevel", LogLevel.Order);
        settings.SaveToFile("c:\\kalitte\\lastrun.json");
        alternates.Set("LogConsole", true);
        alternates.Set("UILoggingLevel", LogLevel.Order);
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



