// algo
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZedGraph;

namespace Kalitte.Trading.DataVisualization
{
    public class ChartSerie
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public PointPairList Points = new PointPairList();
        public Color Color { get; set; } = Color.Black;

        public ChartSerie SetColor(Color color)
        {
            this.Color = color;
            return this;
        }

        public void Add(DateTime t, decimal value)
        {
            Points.Add(new XDate(t), (double)value);
        }
    }

    public class Chart
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public Dictionary<string, ChartSerie> Series { get; set; } = new Dictionary<string, ChartSerie>();
        
        public ChartSerie Serie(string name, string title = "")
        {
            if (Series.TryGetValue(name, out ChartSerie serie)) return serie;
            serie = new ChartSerie();
            serie.Name = name;
            serie.Title = title;
            Series.Add(name, serie);
            return serie;
        }



        public void Save(string fileName, bool clear = true)
        {
            GraphPane myPane = new GraphPane(new RectangleF(0, 0, 3200, 2400), Title ?? Name, "Time", "Value");
            foreach (var item in Series)
            {
                myPane.AddCurve(item.Value.Title ?? item.Value.Name, item.Value.Points, item.Value.Color, SymbolType.None);
            }
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
            im.Save(fileName, ImageFormat.Png);     
            if (clear) Clear(); 
        }

        internal void Clear()
        {
            Series.Clear(); 
        }
    }
}
