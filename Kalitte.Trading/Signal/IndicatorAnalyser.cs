// algo
using Kalitte.Trading.Algos;
using Kalitte.Trading.Indicators;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class IndicatorAnalyserResult : SignalResult
    {
        public IndicatorAnalyserResult(SignalBase signal, DateTime signalTime) : base(signal, signalTime)
        {

        }

        public decimal? Value { get; set; }
        public decimal Speed { get; set; }
        public decimal Acceleration { get; set; }
    }

    internal class IndicatorAnalyser : AnalyserBase<AnalyserConfig>
    {
        public ITechnicalIndicator i1k;

        public IndicatorAnalyser(string name, string symbol, AlgoBase owner, AnalyserConfig config) : base(name, symbol, owner, config)
        {

        }

        //protected override void LoadNewBars(object sender, ListEventArgs<IQuote> e)
        //{
        //    var seconds = Algo.GetSymbolPeriodSeconds(i1k.InputBars.Period.ToString());
        //    AnalyseList.ResetSpeed(AnalyseList.LastValue, Algo.Now.AddSeconds(0));
        //    SaveSpeed(Algo.Now);
        //    Console.WriteLine($"BAR LOADED:  {i1k.Results.Last().Date}/{i1k.Results.Last().Value} { Algo.Now } - {AnalyseList.LastValue}");
        //}

        public override void Init()
        {
            this.Indicators.Add(i1k);
            this.i1k.InputBars.ListEvent += base.InputbarsChanged;
            var lastResult = i1k.Results.Last();            
            base.Init();            
            //var seconds = Algo.GetSymbolPeriodSeconds(i1k.InputBars.Period.ToString());
            //AnalyseList.ResetSpeed(lastResult.Value.Value, Algo.Now);



        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? Algo.Now;
            var result = new IndicatorAnalyserResult(this, time );

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp, time);

            if (CollectList.Ready && mp > 0)
            {
                decimal mpAverage = CollectList.LastValue;
                var l1 = i1k.NextValue(mpAverage).Value.Value;
                AnalyseList.Collect(l1, time);

                if (!AnalyseList.SpeedInitialized)
                {
                    AnalyseList.ResetSpeed(AnalyseList.LastValue, time);
                    AnalyseList.Speed.ResetSpeed(0, time);
                }                

                result.Value = AnalyseList.LastValue;
                result.Speed = AnalyseList.CalculateSpeed(time);
                AnalyseList.UpdateSpeed(time, result.Value.Value);
                AnalyseList.Speed.UpdateSpeed(time, result.Speed);
                result.Acceleration = AnalyseList.Speed.CalculateSpeed(time);
                if (time.Second % 5 == 0)
                {
                    //Console.WriteLine($"{AnalyseList.SpeedStart}/{AnalyseList.SpeedInitialValue} - {result.SignalTime}, {result.Speed} {result.Value}");
                    Chart("Derivs").Serie("Speed").SetColor(Color.Black).Add(time, result.Speed);
                    Chart("Derivs").Serie("Acceleration").SetColor(Color.Green).Add(time, result.Acceleration);
                    Chart("Value").Serie("Value").SetColor(Color.Green).Add(time, result.Value.Value);                    
                }

                if (time.Minute == 1 && time.Second == 1)
                {
                    SaveCharts(time);                                                        
                }
            }

            if (mp > 0)
            {
                TrackAnalyseList(time);
                TrackCollectList(time, mp);
            }

            return result;
        }
    }
}
