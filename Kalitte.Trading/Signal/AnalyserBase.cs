﻿// algo
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

    public class AnalyserBase : Signal
    {

        public DateTime TrackStart { get; set; } = DateTime.MaxValue;
        public DateTime TrackEnd { get; set; } = DateTime.MinValue;
        PointPairList analyseValues = new PointPairList();
        PointPairList collectedValues = new PointPairList();
        PointPairList collectRawValues = new PointPairList();
        public int TrackId { get; set; }


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

        public void TrackCollectList(DateTime t, decimal collectValue)
        {
            if (TrackStart <= t && TrackEnd >= t)
            {
                if (CollectList.Ready)
                {
                    collectedValues.Add(new XDate(t), (double)CollectList.LastValue);
                    collectRawValues.Add(new XDate(t),(double)collectValue);                     
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
                im.Save($"c:\\kalitte\\{this.Name}-collect-{TrackStart.ToString("HH-mm-ss")}-{TrackEnd.ToString("HH-mm-ss")}.png", ImageFormat.Png);
                collectedValues.Clear();
                collectRawValues.Clear();
            }
        }

        public void TrackAnalyseList(DateTime t)
        {
            if (TrackStart <= t && TrackEnd >= t)
            {
                if (AnalyseList.Ready) analyseValues.Add(new XDate(t), (double)AnalyseList.LastValue);
            } else if (TrackStart != DateTime.MaxValue && analyseValues.Count > 0)
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
                im.Save($"c:\\kalitte\\{this.Name}-al-{TrackStart.ToString("HH-mm-ss")}-{TrackEnd.ToString("HH-mm-ss")}.png", ImageFormat.Png);
                analyseValues.Clear();
            }
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
