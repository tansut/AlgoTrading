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
        settings.Start = new DateTime(2022, 3, 1);
        settings.Finish = new DateTime(2022, 3, 18);
        settings.AutoClosePositions = true;
        settings.AutoClosePositions = true;

        var initValues = AlgoBase.GetConfigValues(typeof(Bist30));
        var alternates = settings.Alternates = new AlternateValues(initValues);


        alternates.Set("UsePerformanceMonitor", false);

        // analyser defaults
        alternates.Set("DataCollectSize", 8);
        alternates.Set("DataAnalysisSize", 80);
        alternates.Set("DataCollectAverage", Average.Ema);
        alternates.Set("DataAnalysisAverage", Average.Sma);

        // profit && loss

        alternates.Set("PriceLowLimit", 2300);
        alternates.Set("PriceHighLimit", 2400);

        alternates.Set("Profit/InitialQuantity", 50);
        alternates.Set("Profit/KeepQuantity", 20);
        alternates.Set("Profit/QuantityStep", 10);
        alternates.Set("Profit/QuantityStepMultiplier", 1);
        alternates.Set("Profit/StartAt", 0.5);        
        alternates.Set("Profit/Step", 0.1);

        
        // loss
        alternates.Set("RsiLoss/InitialQuantity", 80);
        alternates.Set("RsiLoss/QuantityStep", 10);
        alternates.Set("RsiLoss/KeepQuantity", 0);
        alternates.Set("RsiLoss/StartAt", 0.9);
        alternates.Set("RsiLoss/Step", 0.1);

        // rsi
        alternates.Set("Rsi", 14);

        alternates.Set("RsiOrderHighL1/Make", 1);
        alternates.Set("RsiOrderHighL1/Action", RsiPositionAction.IfEmpty);
        alternates.Set("RsiOrderHighL1/L1", 73.00);
        alternates.Set("RsiOrderHighL1/L2", 76.50);

        alternates.Set("RsiOrderHighL2/Keep", 1);
        alternates.Set("RsiOrderHighL2/Make", 3);
        alternates.Set("RsiOrderHighL2/Action", RsiPositionAction.Additional);
        alternates.Set("RsiOrderHighL2/L1", 76.51);
        alternates.Set("RsiOrderHighL2/L2", 82.50);

        alternates.Set("RsiOrderHighL3/Make", 4);
        alternates.Set("RsiOrderHighL3/Action", RsiPositionAction.Radical);
        alternates.Set("RsiOrderHighL3/L1", 82.51);
        alternates.Set("RsiOrderHighL3/L2", 100);


        alternates.Set("RsiOrderLowL1/Make", 1);
        alternates.Set("RsiOrderLowL1/Action", RsiPositionAction.IfEmpty);
        alternates.Set("RsiOrderLowL1/L1", 33.00);
        alternates.Set("RsiOrderLowL1/L2", 29.00);

        alternates.Set("RsiOrderLowL2/Keep", 1);
        alternates.Set("RsiOrderLowL2/Make", 3);
        alternates.Set("RsiOrderLowL2/Action", RsiPositionAction.Additional);
        alternates.Set("RsiOrderLowL2/L1", 28.99);
        alternates.Set("RsiOrderLowL2/L2", 23.00);

        alternates.Set("RsiOrderLowL3/Make", 4);
        alternates.Set("RsiOrderLowL3/Action", RsiPositionAction.Radical);
        alternates.Set("RsiOrderLowL3/L1", 22.99);
        alternates.Set("RsiOrderLowL3/L2", 0);
        alternates.Set("RsiOrderowL3/Usage", OrderUsage.CreatePosition);

        alternates.Set("RsiGradientTolerance", 0.02);
        alternates.Set("RsiGradientLearnRate", 0.005);

        // volume power signal
        alternates.Set("PowerLookback", 5);

        // ma cross
        
        alternates.Set("MovPeriod", 5);
        alternates.Set("MovPeriod2", 9);

        alternates.Set("CrossL1/Enabled", true);
        alternates.Set("CrossL1/AvgChange", 0.32M);
        alternates.Set("CrossL1/Dynamic", true);
        alternates.Set("CrossL1/PowerThreshold", 88);
        alternates.Set("CrossL1/PowerNegativeMultiplier", 1.3);
        alternates.Set("CrossL1/PowerPositiveMultiplier", 2.8);
        alternates.Set("CrossL1/Quantity", 4);
        alternates.Set("CrossL1/RsiMax", 55.6);
        alternates.Set("CrossL1/RsiMin", 45.4);

        alternates.Set("CrossL2/Enabled", true);
        alternates.Set("CrossL2/AvgChange", 0.32M);
        alternates.Set("CrossL2/Dynamic", true);
        alternates.Set("CrossL2/PowerThreshold", 88);
        alternates.Set("CrossL2/PowerNegativeMultiplier", 1.3);
        alternates.Set("CrossL2/PowerPositiveMultiplier", 2.8);
        alternates.Set("CrossL2/Quantity", 8);
        alternates.Set("CrossL2/RsiMax", 52.6);
        alternates.Set("CrossL2/RsiMin", 48.4);

        // cross rsi
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
        alternates.Set("LoggingLevel", LogLevel.Debug);
        alternates.Set("LogConsole", true);
        alternates.Set("UILoggingLevel", LogLevel.Warning);
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



