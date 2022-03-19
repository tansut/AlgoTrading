using System;
using System.Collections.Generic;
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
        PointPairList Points = new PointPairList();
    }

    public class Chart
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public  List<ChartSerie> Series { get; set; } = new List<ChartSerie> { };
    }
}
