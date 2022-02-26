// algo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;

namespace Kalitte.Trading
{

    public enum PowerRatio
    {
        Unknown,
        Critical,
        High,
        Average,
        BelowAverage,
        Low
    }

    public class PowerSignalResult : SignalResult
    {
        internal double CurrentVolume;

        public decimal Value { get; set; }
        public PowerRatio Power { get
            {
                if (Value == 0) return PowerRatio.Unknown;
                if (Value < 20) return PowerRatio.Low;
                else if (Value < 40) return PowerRatio.BelowAverage;
                else if (Value < 60) return PowerRatio.Average;
                else if (Value < 80) return PowerRatio.High;
                else return PowerRatio.Critical;
            }
        }

        public double VolumePerSecond { get; internal set; }
        //public double Strenght { get; internal set; }
        public decimal LastVolume { get; internal set; }

        public PowerSignalResult(Signal signal, DateTime t) : base(signal, t)
        {
            this.finalResult = BuySell.Sell;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | [{Power}] Rsi:{Value} Volume/sec: {VolumePerSecond} Volume/period: {CurrentVolume} Prev Volume:{LastVolume}";
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

    }

    public class PowerSignal : Signal
    {
        public ITechnicalIndicator Indicator { get; set; }
        private FinanceBars volumeBars;

        public int VolumeCollectionPeriod { get; set; } = 5;



        public override void Init()
        {
            volumeBars = new FinanceBars(VolumeCollectionPeriod);
            base.Init();
        }

        protected override void LoadNewBars(object sender, ListEventArgs<Skender.Stock.Indicators.IQuote> e)
        {
            
        }


        public PowerSignal(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {
            
        }

        protected override void ResetInternal()
        {
            volumeBars.Clear();
        }

        double calculateVolumeBySecond(DateTime t, decimal volume)
        {
            //Helper.SymbolSeconds(Indicator.InputBars.Period.ToString(), out int periodSeconds);
            Helper.SymbolSeconds(Algo.SymbolPeriod.ToString(), out int periodSeconds);
            var rounded = Helper.RoundDown(t, TimeSpan.FromSeconds(periodSeconds));
            var elapsedSeconds = Math.Max(1, (t - rounded).TotalSeconds);
            //Log($"Volume Calc: per: {periodSeconds} seconds: {elapsedSeconds} volume: {volume}", LogLevel.Verbose);
            if (elapsedSeconds > 15) return (double)volume / elapsedSeconds;
            else return 0;
        }

        void calculatePower(PowerSignalResult s, DateTime t)
        {
            var volumeAvg = volumeBars.List.GetEma(Math.Min(volumeBars.Count, VolumeCollectionPeriod), CandlePart.Volume).Last().Ema.Value;
            var volumePerSecond = (double)volumeAvg;
            Helper.SymbolSeconds(Indicator.InputBars.Period.ToString(), out int periodSeconds);
            var volume = volumePerSecond * periodSeconds;
            var value = Indicator.NextValue((decimal)volume);
            var last = Indicator.UsedInput.Last().Close;
            s.Value = value;
            s.VolumePerSecond = volumePerSecond;
            s.CurrentVolume = volume;
            s.LastVolume = last;
        }

        protected override SignalResult CheckInternal(DateTime? t = null)
        {

            var time = t ?? DateTime.Now;

            var result = new PowerSignalResult(this, time);
            
            var mp = Algo.GetVolume(Symbol, Indicator.InputBars.Period, t);
            if (mp > 0)
            {
                var volume = calculateVolumeBySecond(time, mp);
                if (volume > 0)
                {
                    var q = new MyQuote() { Date = time, Volume = (decimal)volume };
                    volumeBars.Push(q);
                }
                else
                {
                    Log($"Volume is zero", LogLevel.Verbose, time);
                    return result;
                }
            }
            else return result;
            if (volumeBars.Count  > 0)  calculatePower(result, time);
            return result;
        }



    }


}
