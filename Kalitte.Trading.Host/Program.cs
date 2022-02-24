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


public class Settings
{
    public DateTime sDate { get; set; } = new DateTime(2022, 02, 23, 9, 30, 0);
    public DateTime fDate { get; set; } = new DateTime(2022, 02, 23, 23, 0, 0);
    public AlternateValues Alternates { get; set; }
}

public class Program
{
    //public static DateTime sDate = new DateTime(2022, 02, 23, 9, 30, 0);
    //public static DateTime fDate = new DateTime(2022, 02, 23, 23, 0, 0);

    //public static void DoBacktest(Dictionary<string, object> initValues)
    //{
    //    //var initValues = AlgoBase.GetProperties(typeof(MyAlgo));
    //    var algo = new MyAlgo(initValues);
    //    var test = new Backtest(algo, sDate, fDate);
    //    Console.WriteLine("Backtest started for {}");
    //    test.Start();
    //}


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
        settings.sDate = new DateTime(2022, 02, 21, 9, 30, 0);
        settings.fDate = new DateTime(2022, 02, 21, 23, 0, 0);

        var initValues = AlgoBase.GetProperties(typeof(Bist30Futures));
        var alternates = settings.Alternates = new AlternateValues(initValues);
        
        //alternates.Set("LogConsole", true);
        alternates.Set("LoggingLevel", LogLevel.Order);
        //alternates.Set("Rsi", 14);
        alternates.Set("PowerLookback", 5);
        alternates.Set("ProfitQuantity", 0);
        alternates.Set("RsiHighLimit", 0);
        alternates.Set("RsiLowLimit", 0);
        alternates.Set("DynamicCross", true);






        //alternates.Set("MinRsiChange", 1M 2M);
        //alternates.Set("MaAvgChange", 0.25M);
        //alternates.Set("MaPeriods", 25);
        //alternates.Set("CrossPriceCollectionPeriod", 2);
        //alternates.Set("PowerVolumeCollectionPeriod", 10);
        //alternates.Set("PowerCrossThreshold", 50);
        //alternates.Set("ExpectedNetPl", 10M);

        return settings;
    }

    public static void Main(string[] args)
    {
        //string execPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        ////var alternates2 = LoadFromFile(Path.Combine(execPath, "alternates.json"));

        //var settings1 = new Settings();
        //var initValues1 = AlgoBase.GetProperties(typeof(MyAlgo));
        //settings1.Alternates = new AlternateValues(initValues1); 
        //SaveToFile("./settings.json", settings1);
        //return;

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

            settings = LoadFromFile(args[0]);
            var firstValues = settings.Alternates.Lean();
            settings.Alternates = new AlternateValues(firstValues);

        }


        var optimize = new Optimizer<Bist30Futures>(settings.sDate, settings.fDate, typeof(Bist30Futures));

        optimize.Start(settings.Alternates);



    }

}

