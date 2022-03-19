using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.DataVisualization
{
    public class ChartList
    {
        Dictionary<string, Chart> charts = new Dictionary<string, Chart>();

        public Chart Chart(string name, string title = "")
        {
            if (charts.TryGetValue(name, out Chart chart))  return chart;
            chart = new Chart();
            chart.Name = name;
            chart.Title = title;
            charts.Add(name, chart);
            return chart;
        }
    }
}
