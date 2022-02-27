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
        public List<string> Filters { get; set; } = new List<string>();
        public decimal DefaultChange { get; set; } = 10.0M;


        public MonitorItemDefinition StartMonitoring(string name, decimal change)
        {
            var existing = Definitions.ContainsKey(name);
            if (!existing) Definitions[name] = new MonitorItemDefinition() {  ChangePercent = change };
            return Definitions[name];
        }

        public void RaiseEvent(MonitorEventArgs args)
        {
            var raise = true;
            if (Filters.Count > 0)
            {
                var f = Filters.Where(p => args.Item.Name.StartsWith(p)).FirstOrDefault();
                raise = f != null;
            }
            if (raise && MonitorEvent != null) MonitorEvent(this, args);
        }

        public MonitorItem Set(string name, decimal value)
        {
            Definitions.TryGetValue(name, out var existing);
            if (existing == null)
            {
                existing = StartMonitoring(name, DefaultChange);
            }
            if (existing != null)
            {
                Items.TryGetValue(name, out var item);
                if (item == null)
                {
                    item = new MonitorItem() { Definition = existing, Name = name, CurrentValue = value };
                    Items.Add(name, item);
                    RaiseEvent(new MonitorEventArgs() { Item = item, EventType = MonitorEventType.Initialized });
                } else
                {
                    var change = 100.0M * (value - item.CurrentValue) / item.CurrentValue;
                    item.OldValue = item.CurrentValue;
                    item.CurrentValue = value;
                    if (Math.Abs(change) >= existing.ChangePercent)
                    {
                        RaiseEvent(new MonitorEventArgs() { Change = change, Item = item, EventType = MonitorEventType.Updated });                
                    }
                }
                return item;
            } return null;
        }
    }
}
