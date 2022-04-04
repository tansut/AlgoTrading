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


        public override void Init()
        {
            this.Indicators.Add(i1k);
            this.i1k.InputBars.ListEvent += base.InputbarsChanged;
            var lastResult = i1k.Results.Last();            
            base.Init();            
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            var time = t ?? Algo.Now;
            var result = new IndicatorAnalyserResult(this, time );

            var mp = Algo.GetMarketPrice(Symbol, t);

            if (mp > 0) CollectList.Collect(mp, time);

            if (CollectList.Ready && mp > 0)
            {
                decimal mpAverage = CollectList.LastValue();
                var l1 = i1k.NextValue(mpAverage).Value.Value;
                AnalyseList.Collect(l1, time);
                result.Value = AnalyseList.LastValue(Lookback);

                //if (!AnalyseList.SpeedInitialized)
                //{
                //    AnalyseList.ResetSpeed(AnalyseList.LastValue, time);
                //    AnalyseList.Speed.ResetSpeed(0, time);
                //}                


                //result.Speed = AnalyseList.CalculateSpeed(time);
                //AnalyseList.UpdateSpeed(time, result.Value.Value);
                //AnalyseList.Speed.UpdateSpeed(time, result.Speed);
                //result.Acceleration = AnalyseList.Speed.CalculateSpeed(time);
                if (time.Second % 5 == 0 && Algo.Simulation)
                {
                    //Chart("Derivs").Serie("Speed").SetColor(Color.Black).Add(time, result.Speed);
                    //Chart("Derivs").Serie("Acceleration").SetColor(Color.Green).Add(time, result.Acceleration);
                    Chart("Value").Serie("Sma").SetColor(Color.Green).Add(time, result.Value.Value);
                    Chart("Value").Serie("Value").SetColor(Color.Black).Add(time, l1);
                }

                if (time.Minute == 1 && time.Second == 1 && Algo.Simulation)
                {
                    SaveCharts(time);
                }
            }

            //if (mp > 0)
            //{
            //    TrackAnalyseList(time);
            //    TrackCollectList(time, mp);
            //}

            return result;
        }
    }
}
