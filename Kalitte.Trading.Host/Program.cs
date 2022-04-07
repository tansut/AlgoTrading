using Kalitte.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
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
        settings.Finish = new DateTime(2022, 3, 31);
        settings.AutoClosePositions = true;


        var initValues = AlgoBase.GetConfigValues(typeof(Bist30));
        var alternates = settings.Alternates = new AlternateValues(initValues);


        alternates.Set("UsePerformanceMonitor", false);

        // initial portfolio
        alternates.Set("Portfolio/Quantity", 0);
        alternates.Set("Portfolio/Side", BuySell.Sell);

        // global order
        alternates.Set("Orders/Total", 10);
        alternates.Set("Orders/PLEnabled", true);
        alternates.Set("Orders/PL", new decimal[] { -200, -150, 100, 150, 200 });
        alternates.Set("Orders/PLMultiplier", new decimal[] { 0, 0.6M, 1, 0.4M, 0.2M, 0 });


        alternates.Set("Orders/NightRatio", 0.4);
        alternates.Set("Orders/KeepRatio", 0.2);
        alternates.Set("Orders/KeepSide", ClosePositionSide.UseCross);
        alternates.Set("DailyClose/Enabled", true);

        // analyser defaults
        alternates.Set("DataCollectSize", 6);
        alternates.Set("DataAnalysisSize", 60);
        alternates.Set("DataCollectAverage", Average.Ema);
        alternates.Set("DataAnalysisAverage", Average.Ema);
        alternates.Set("DataAnalysisLookback", 30);
        alternates.Set("DataAnalysisPeriods", BarPeriod.Sec10);

        // profit && loss

        alternates.Set("PriceLowLimit", 2300);
        alternates.Set("PriceHighLimit", 2400);

        alternates.Set("Profit/InitialQuantity", 50);
        alternates.Set("Profit/QuantityStep", 10);
        alternates.Set("Profit/KeepQuantity", 30);
        alternates.Set("Profit/QuantityStepMultiplier", 1);
        alternates.Set("Profit/StartAt", 0.48);
        alternates.Set("Profit/Step", 0.18);

        alternates.Set("GlobalLoss/InitialQuantity", 50);
        alternates.Set("GlobalLoss/QuantityStep", 25);
        alternates.Set("GlobalLoss/KeepQuantity", 0);
        alternates.Set("GlobalLoss/StartAt", 1.0);
        alternates.Set("GlobalLoss/Step", 0.25);


        // rsi loss
        alternates.Set("RsiLoss/Enabled", false);
        alternates.Set("RsiLoss/InitialQuantity", 80);
        alternates.Set("RsiLoss/QuantityStep", 10);
        alternates.Set("RsiLoss/KeepQuantity", 0);
        alternates.Set("RsiLoss/StartAt", 0.7);
        alternates.Set("RsiLoss/Step", 0.15);

        // rsi
        alternates.Set("Rsi", 14);

        alternates.Set("RsiOrderHighL1/Enabled", false);
        alternates.Set("RsiOrderHighL1/MakeRatio", 0);
        alternates.Set("RsiOrderHighL1/Action", RsiPositionAction.IfEmpty);
        alternates.Set("RsiOrderHighL1/L1", 73.00);
        alternates.Set("RsiOrderHighL1/L2", 76.50);

        alternates.Set("RsiOrderHighL2/Enabled", true);
        alternates.Set("RsiOrderHighL2/KeepRatio", 0.1);
        alternates.Set("RsiOrderHighL2/MakeRatio", 0);
        alternates.Set("RsiOrderHighL2/Action", RsiPositionAction.Additional);
        alternates.Set("RsiOrderHighL2/L1", 76.51);
        alternates.Set("RsiOrderHighL2/L2", 84.50);

        alternates.Set("RsiOrderHighL3/Enabled", true);
        alternates.Set("RsiOrderHighL3/MakeRatio", 0.1);
        alternates.Set("RsiOrderHighL3/Action", RsiPositionAction.Radical);
        alternates.Set("RsiOrderHighL3/L1", 84.51);
        alternates.Set("RsiOrderHighL3/L2", 100);        

        alternates.Set("RsiOrderLowL1/Enabled", false);
        alternates.Set("RsiOrderLowL1/MakeRatio", 0.1);
        alternates.Set("RsiOrderLowL1/ActionRatio", RsiPositionAction.IfEmpty);
        alternates.Set("RsiOrderLowL1/L1", 33.00);
        alternates.Set("RsiOrderLowL1/L2", 29.00);

        alternates.Set("RsiOrderLowL2/Enabled", true);
        alternates.Set("RsiOrderLowL2/KeepRatio", 0.1);
        alternates.Set("RsiOrderLowL2/MakeRatio", 0);
        alternates.Set("RsiOrderLowL2/Action", RsiPositionAction.Additional);
        alternates.Set("RsiOrderLowL2/L1", 28.99);
        alternates.Set("RsiOrderLowL2/L2", 23.00);

        alternates.Set("RsiOrderLowL3/Enabled", true);
        alternates.Set("RsiOrderLowL3/MakeRatio", 0.1);
        alternates.Set("RsiOrderLowL3/ActionRatio", RsiPositionAction.Radical);
        alternates.Set("RsiOrderLowL3/L1", 22.99);
        alternates.Set("RsiOrderLowL3/L2", 0);
        alternates.Set("RsiOrderLowL3/Usage", OrderUsage.CreatePosition);

        alternates.Set("RsiGradientTolerance", 0.02);
        alternates.Set("RsiGradientLearnRate", 0.005);

        // volume power signal
        alternates.Set("PowerLookback", 5);

        // ma cross

        alternates.Set("MovPeriod", 5);
        alternates.Set("MovPeriod2", 9);

        alternates.Set("CrossL1/Enabled", true);
        alternates.Set("CrossL1/AvgChange", 0.6M);
        alternates.Set("CrossL1/PreChange", 0);
        alternates.Set("CrossL1/Dynamic", true);
        alternates.Set("CrossL1/PreOrder", 0.4);        
        alternates.Set("CrossL1/PowerThreshold", 88);
        alternates.Set("CrossL1/PowerNegativeMultiplier", 1.3);
        alternates.Set("CrossL1/PowerPositiveMultiplier", 2.8);
        alternates.Set("CrossL1/QuantityRatio", 1);
        alternates.Set("CrossL1/RsiLongEnabled", true);
        alternates.Set("CrossL1/RsiLong", new[] { 50M, 55M, 60 });
        alternates.Set("CrossL1/RsiLongMultiplier", new[] { 0.6M, 1M, 0.6M, 1M });

        alternates.Set("CrossL1/RsiShortEnabled", true);
        alternates.Set("CrossL1/RsiShort", new[] { 40, 45M, 55});
        alternates.Set("CrossL1/RsiShortMultiplier", new[] { 1M, 0.6M, 1M, 0.6M });     



        // System
        alternates.Set("LoggingLevel", LogLevel.Verbose);
        alternates.Set("Symbol", "F_XU0300422");
        alternates.Set("SymbolPeriod", BarPeriod.Min10);

        // save to read to use files.
        alternates.Set("LogConsole", false);
        alternates.Set("UILoggingLevel", LogLevel.Debug);
        alternates.SaveToFile($"c:\\kalitte\\Bist30-test.json");

        // lastrun
        alternates.Set("LoggingLevel", LogLevel.Order);
        settings.SaveToFile("c:\\kalitte\\lastrun.json");
        settings = OptimizerSettings.LoadFromFile("c:\\kalitte\\lastrun.json");
        settings.Alternates.Set("LoggingLevel", LogLevel.Debug);
        settings.Alternates.Set("LogConsole", true);
        settings.Alternates.Set("UILoggingLevel", LogLevel.Warning);
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
            }
            else
            {
                Console.WriteLine("Unknown option");
                return;
            }
        }

        var optimize = new Optimizer<Bist30>(settings, typeof(Bist30));
        optimize.Start();
    }
}



