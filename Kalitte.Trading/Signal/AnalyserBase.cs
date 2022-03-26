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
using ZedGraph;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kalitte.Trading
{
    public class AnalyserConfig : SignalConfig
    {
        [AlgoParam(0, "AnalyseSize")]
        public int AnalyseSize { get; set; }

        [AlgoParam(0, "CollectSize")]
        public int CollectSize { get; set; }

        [AlgoParam(Average.Ema)]
        public Average CollectAverage { get; set; }

        [AlgoParam(Average.Sma)]
        public Average AnalyseAverage { get; set; }

        [AlgoParam(120)]
        public int Lookback { get; set; }

        [AlgoParam(BarPeriod.Sec)]
        public BarPeriod AnalysePeriod { get; set; }
    }

    public class AnalyserBase<C> : Signal<C> where C : AnalyserConfig
    {

        public DateTime TrackStart { get; set; } = DateTime.MaxValue;
        public DateTime TrackEnd { get; set; } = DateTime.MinValue;
        PointPairList analyseValues = new PointPairList();
        PointPairList collectedValues = new PointPairList();
        PointPairList collectRawValues = new PointPairList();
        public PointPairList SpeedValues = new PointPairList();
        public int TrackId { get; set; }


        public int CollectSize { get; set; }
        public int AnalyseSize { get; set; }
        public int Lookback { get; set; }





        public AnalyseList CollectList { get; set; }
        public AnalyseList AnalyseList { get; set; }

        

        public AnalyserBase(string name, string symbol, AlgoBase owner, C config) : base(name, symbol, owner, config)
        {

        }

        public void TrackSpeed(DateTime t, decimal speed)
        {
            SpeedValues.Add(new XDate(t), (double)speed);
        }

        public void SaveSpeed(DateTime t)
        {

            GraphPane myPane = new GraphPane(new RectangleF(0, 0, 3200, 2400), "Unscented Kalman Filter", "number", "measurement");
            myPane.AddCurve("collectlist", SpeedValues, Color.Green, SymbolType.None);            

            myPane.XAxis.Type = AxisType.Date;
            myPane.XAxis.Scale.Format = "hh:mm";
            myPane.XAxis.Scale.FontSpec.Angle = 60;
            myPane.XAxis.Scale.FontSpec.Size = 12;
            myPane.XAxis.Scale.MajorUnit = DateUnit.Hour;
            myPane.XAxis.Scale.MajorStep = 1;
            myPane.XAxis.Scale.MinorUnit = DateUnit.Minute;
            myPane.XAxis.Scale.MinorStep = 1;
            Bitmap bm = new Bitmap(200, 200);
            Graphics g = Graphics.FromImage(bm);
            myPane.AxisChange(g);
            Image im = myPane.GetImage();
            im.Save(Path.Combine(Algo.LogDir, $"{this.Name}-speed-{t.ToString("HH-mm-ss")}.png"), ImageFormat.Png);
            SpeedValues.Clear();            
        }


        public void TrackCollectList(DateTime t, decimal collectValue)
        {
            if (TrackStart <= t && TrackEnd >= t)
            {
                if (CollectList.Ready)
                {
                    collectedValues.Add(new XDate(t), (double)CollectList.LastValue());
                    collectRawValues.Add(new XDate(t), (double)collectValue);
                }
            }
            else if (TrackStart != DateTime.MaxValue && collectedValues.Count > 0)
            {
                GraphPane myPane = new GraphPane(new RectangleF(0, 0, 3200, 2400), "Unscented Kalman Filter", "number", "measurement");
                myPane.AddCurve("collectlist", collectedValues, Color.Green, SymbolType.None);
                myPane.AddCurve("rawlist", collectRawValues, Color.Red, SymbolType.None);

                myPane.XAxis.Type = AxisType.Date;
                myPane.XAxis.Scale.Format = "hh:mm";
                myPane.XAxis.Scale.FontSpec.Angle = 60;
                myPane.XAxis.Scale.FontSpec.Size = 12;
                myPane.XAxis.Scale.MajorUnit = DateUnit.Hour;
                myPane.XAxis.Scale.MajorStep = 1;
                myPane.XAxis.Scale.MinorUnit = DateUnit.Minute;
                myPane.XAxis.Scale.MinorStep = 1;
                Bitmap bm = new Bitmap(200, 200);
                Graphics g = Graphics.FromImage(bm);
                myPane.AxisChange(g);
                Image im = myPane.GetImage();
                im.Save(Path.Combine(Algo.LogDir, $"{this.Name}-collect-{TrackStart.ToString("HH-mm-ss")}-{TrackEnd.ToString("HH-mm-ss")}.png"), ImageFormat.Png);
                collectedValues.Clear();
                collectRawValues.Clear();
            }
        }

        public void TrackAnalyseList(DateTime t)
        {
            if (TrackStart <= t && TrackEnd >= t)
            {
                if (AnalyseList.Ready) analyseValues.Add(new XDate(t), (double)AnalyseList.LastValue());
            }
            else if (TrackStart != DateTime.MaxValue && analyseValues.Count > 0)
            {
                GraphPane myPane = new GraphPane(new RectangleF(0, 0, 3200, 2400), "Unscented Kalman Filter", "number", "measurement");
                myPane.AddCurve("current", analyseValues, Color.Green, SymbolType.None);
                myPane.XAxis.Type = AxisType.Date;
                myPane.XAxis.Scale.Format = "hh:mm";
                myPane.XAxis.Scale.FontSpec.Angle = 60;
                myPane.XAxis.Scale.FontSpec.Size = 12;
                myPane.XAxis.Scale.MajorUnit = DateUnit.Hour;
                myPane.XAxis.Scale.MajorStep = 1;
                myPane.XAxis.Scale.MinorUnit = DateUnit.Minute;
                myPane.XAxis.Scale.MinorStep = 1;

                Bitmap bm = new Bitmap(200, 200);
                Graphics g = Graphics.FromImage(bm);
                myPane.AxisChange(g);
                Image im = myPane.GetImage();
                im.Save(Path.Combine(Algo.LogDir, $"{this.Name}-al-{TrackStart.ToString("HH-mm-ss")}-{TrackEnd.ToString("HH-mm-ss")}.png"), ImageFormat.Png);
                analyseValues.Clear();
            }
        }


        protected virtual void AdjustSensitivityInternal(double ratio, string reason)
        {
            //AnalyseSize = Config.AnalyseSize + Convert.ToInt32((Config.AnalyseSize * (decimal)ratio));
            //CollectSize = Config.InitialCollectSize + Convert.ToInt32((Config.InitialCollectSize * (decimal)ratio));
            //CollectList.Resize(CollectSize);
            //AnalyseList.Resize(AnalyseSize);
            this.Lookback = Convert.ToInt32(this.Lookback * (decimal)ratio);
            Watch("sensitivity/lookback", (decimal)Lookback);
            //Watch("sensitivity/analysesize", (decimal)AnalyseSize);
        }

        public void AdjustSensitivity(double ratio, string reason)
        {
            Monitor.Enter(OperationLock);
            try
            {
                AdjustSensitivityInternal(ratio, reason);
            }
            finally
            {
                Monitor.Exit(OperationLock);
            }
        }


        public override void Init()
        {
            CollectSize = Convert.ToInt32(Config.CollectSize);
            AnalyseSize = Convert.ToInt32(Config.AnalyseSize);
            Lookback = Convert.ToInt32(Config.Lookback);
            CollectList = new AnalyseList(CollectSize, Config.CollectAverage);
            AnalyseList = new AnalyseList(AnalyseSize, Config.AnalyseAverage);
            AnalyseList.Period = Config.AnalysePeriod;
            ResetInternal();
            MonitorInit("sensitivity/collectsize", (decimal)CollectSize);
            MonitorInit("sensitivity/analysesize", (decimal)AnalyseSize);
            //MonitorInit("sensitivity/ratio", 0);
            base.Init();
        }

        protected override void ResetInternal()
        {
            CollectList.Clear();
            AnalyseList.Clear();
            CollectSize = Convert.ToInt32(Config.CollectSize);
            AnalyseSize = Convert.ToInt32(Config.AnalyseSize);
            Lookback = Convert.ToInt32(Config.Lookback);
            CollectList.Resize(CollectSize);
            AnalyseList.Resize(AnalyseSize);
            base.ResetInternal();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {
            return null;
        }



    }


}
