// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class AlgoParam : Attribute
    {
        public object Value { get; set; } = null;
        public AlgoParam(object val = null)
        {

        }

        public AlgoParam()
        {

        }
    }

    public class AlgoParameter<T> where T: IEquatable<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }

        public AlgoParameter(string name, T value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    public class StringParameter: AlgoParameter<string>
    {
        public StringParameter(string name, string value): base(name, value)
        {

        }
    }

    public class DecimalParameter : AlgoParameter<decimal>
    {
        public DecimalParameter(string name, decimal value) : base(name, value)
        {

        }
    }

    public class IntegerParameter : AlgoParameter<int>
    {
        public IntegerParameter(string name, int value) : base(name, value)
        {

        }
    }

    public class BoolParameter : AlgoParameter<bool>
    {
        public BoolParameter(string name, bool value) : base(name, value)
        {

        }
    }

    public enum StartableState
    {
        StartInProgress,
        Started,
        StopInProgress,
        Stopped

    }

    public enum OHLC
    {
        Open = 0,
        High = 1,
        Low = 2,
        Close = 3,
        Volume = 4,
        WClose = 5,
        Diff = 6,
        DiffPercent = 7,
        Undivided = 8,
        Other = 9
    }

    public enum BuySell
    {
        Buy = 1,
        Sell = 2
    }

    public enum VolatileRatio
    {
        Critical,
        High,
        Average,
        BelowAverage,
        Low
    }

    public enum BarPeriod
    {
        Min = 1,
        Min5 = 2,
        Min10 = 3,
        Min15 = 4,
        Min20 = 5,
        Min30 = 6,
        Min60 = 7,
        Min120 = 8,
        Min180 = 9,
        Min240 = 10,
        Session = 11,
        Day = 12,
        Week = 13,
        Month = 14,
        Year = 15
    }

    public enum OrderIcon
    {
        None = 0,
        Buy = 1,
        Sell = 2,
        Stop = 3,
        ShortSell = 4,
        PositionClose = 5,
        Up = 6,
        Down = 7,
        StopLoss = 8,
        TakeProfit = 9
    }





    public static class Extensions
    {
        public static decimal ToCurrency(this decimal d)
        {
            return Math.Round(d, 2, MidpointRounding.AwayFromZero);
        }
    }

    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,        
        Order = 4,
        Error = 10,
        FinalResult = 15,
        Critical = 20
    }

    public interface ILogProvider
    {
        void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null);
    }

}