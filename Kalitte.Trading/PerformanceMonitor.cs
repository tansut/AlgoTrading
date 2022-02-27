using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class MonitorItem
    {
        public string Name { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal OldValue { get; set; }
        public MonitorItemDefinition Definition { get; set; }

        public override string ToString()
        {
            return $"{Name} [{OldValue}->{CurrentValue}]";
        }
    }

    public class MonitorItemDefinition
    {
        public decimal ChangePercent { get; set; }
    }

    public enum MonitorEventType
    {
        Initialized,
        Removed,
        Updated
    }

    public class MonitorFilter
    {
        public string Filter { get; set; }
        public decimal ChangePercent { get; set; }
    }

    public class MonitorEventArgs
    {
        public MonitorItem Item { get; set; }
        public MonitorEventType EventType { get; set; }
        public decimal Change { get; set; }

        public override string ToString()
        {
            if (EventType == MonitorEventType.Initialized)
                return $"{Item.Name} received [{Item.CurrentValue}]";
            else if (EventType == MonitorEventType.Updated)
                return $"{Item} Change: {Change.ToString("##.##")}%";
            else return base.ToString();
        }
    }

    public class PerformanceMonitor
    {
        public Dictionary<string, MonitorItemDefinition> Definitions { get; set; } = new Dictionary<string, MonitorItemDefinition>();
        public Dictionary<string, MonitorItem> Items { get; set; } = new Dictionary<string, MonitorItem>();
        public event EventHandler<MonitorEventArgs> MonitorEvent;
        public List<MonitorFilter> Filters { get; set; } = new List<MonitorFilter>();
        public decimal DefaultChange { get; set; } = 10.0M;


        public void AddFilter(string filter, decimal change)
        {
            Filters.Add(new MonitorFilter() { ChangePercent = change, Filter = filter });
        }

        //public MonitorItemDefinition StartMonitoring(string name, decimal change)
        //{
        //    var existing = Definitions.ContainsKey(name);
        //    if (!existing) Definitions[name] = new MonitorItemDefinition() {  ChangePercent = change };
        //    return Definitions[name];
        //}

        public void RaiseEvent(MonitorEventArgs args)
        {
            var raise = true;
            if (Filters.Count > 0 && args.EventType == MonitorEventType.Updated)
            {
                var f = Filters.Where(p => args.Item.Name.StartsWith(p.Filter)).FirstOrDefault();
                raise = f != null && Math.Abs(args.Change) > f.ChangePercent;
            }
            if (raise && MonitorEvent != null) MonitorEvent(this, args);
        }

        public void Init(string name, decimal value)
        {
            var item = new MonitorItem() { Name = name, CurrentValue = value };
            Items.Add(name, item);
            RaiseEvent(new MonitorEventArgs() { Item = item, EventType = MonitorEventType.Initialized });

        }


        public void Set(string name, decimal value)
        {
            var item = Items[name];
            decimal change = 0;
            if (item.CurrentValue != value)
            {
                if (item.CurrentValue != 0) change = 100.0M * (value - item.CurrentValue) / item.CurrentValue;
                else change = 100;
                item.OldValue = item.CurrentValue;
                item.CurrentValue = value;
                RaiseEvent(new MonitorEventArgs() { Change = change, Item = item, EventType = MonitorEventType.Updated });

            }
        }

    }
}
