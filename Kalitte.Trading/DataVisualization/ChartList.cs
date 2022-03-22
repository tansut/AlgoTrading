// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading.DataVisualization
{
    public class ChartList
    {
        public Dictionary<string, Chart> Items = new Dictionary<string, Chart>();

        public Chart Chart(string name, string title = "")
        {
            if (Items.TryGetValue(name, out Chart chart))  return chart;
            chart = new Chart();
            chart.Name = name;
            chart.Title = title;
            Items.Add(name, chart);
            return chart;
        }
    }
}
