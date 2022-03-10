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
using Skender.Stock.Indicators;

namespace Kalitte.Trading
{

    public class AnalyserBase : Signal
    {
        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }

        public int InitialAnalyseSize { get; set; }
        public int InitialCollectSize { get; set; }


        public Average CollectAverage { get; set; } = Average.Ema;
        public Average AnalyseAverage { get; set; } = Average.Sma;

        public AnalyseList CollectList { get; set; }
        public AnalyseList AnalyseList { get; set; }

        public virtual decimal SignalSensitivity { get; set; } = 1.0M;

        public AnalyserBase(string name, string symbol, AlgoBase owner) : base(name, symbol, owner)
        {

        }



        protected virtual void AdjustSensitivityInternal(double ratio, string reason)
        {
            AnalyseSize = InitialAnalyseSize + Convert.ToInt32((InitialAnalyseSize * (decimal)ratio));
            
            CollectSize = InitialCollectSize + Convert.ToInt32((InitialCollectSize * (decimal)ratio));
            CollectList.Resize(CollectSize);
            AnalyseList.Resize(AnalyseSize);
            Watch("sensitivity/collectsize", (decimal)CollectSize);
            Watch("sensitivity/analysesize", (decimal)AnalyseSize);
            //Monitor("sensitivity/ratio", (decimal)ratio);
            //Log($"{reason}: Adjusted to (%{((decimal)ratio * 100).ToCurrency()}): c:{CollectSize} a:{AnalyseSize}", LogLevel.Debug);
        }


        public override void Init()
        {
            CollectSize = Convert.ToInt32(CollectSize * SignalSensitivity);
            AnalyseSize = Convert.ToInt32(AnalyseSize * SignalSensitivity);
            InitialAnalyseSize = AnalyseSize;
            InitialCollectSize = CollectSize;
            CollectList = new AnalyseList(CollectSize, CollectAverage);
            AnalyseList = new AnalyseList(AnalyseSize, AnalyseAverage);
            ResetInternal();
            MonitorInit("sensitivity/collectsize", (decimal)CollectSize);
            MonitorInit("sensitivity/analysesize", (decimal)AnalyseSize);
            //MonitorInit("sensitivity/ratio", 0);
            base.Init();
        }

        protected override void ResetInternal()
        {

            CollectList.Resize(CollectSize);
            AnalyseList.Resize(AnalyseSize);
            CollectList.Clear();
            AnalyseList.Clear();
            base.ResetInternal();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            return null;
        }



    }


}
