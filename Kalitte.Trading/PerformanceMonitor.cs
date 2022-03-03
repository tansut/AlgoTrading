// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalitte.Trading
{
    public class MonitorItem
    {
        public string Name { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? OldValue { get; set; }
        public MonitorItemDefinition Definition { get; set; }

        public override string ToString()
        {
            var oldVal = OldValue.HasValue ? OldValue.Value.ToString("0.000000") : "null";
            return $"{Name} [{oldVal}->{CurrentValue.ToString("0.0000")}]";
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
        public StartableState State { get; set; } = StartableState.Stopped;
        protected ReaderWriterLock rwl = new ReaderWriterLock();
        protected System.Timers.Timer _timer = null;

        public void AddFilter(string filter, decimal change)
        {
            Filters.Add(new MonitorFilter() { ChangePercent = change, Filter = filter });
        }

        public virtual void Start()
        {
            this.State = StartableState.StartInProgress;
            try
            {

                _timer = new System.Timers.Timer(1000);
                _timer.Elapsed += _timer_Elapsed; ;
                _timer.Start();

                this.State = StartableState.Started;
            }
            finally
            {
            }



            //collectorTaskTokenSource = new CancellationTokenSource();
            //collectorTask = new Task(() =>
            //{
            //    collectorTaskTokenSource.Token.ThrowIfCancellationRequested();                
            //    while (!collectorTaskTokenSource.Token.IsCancellationRequested)
            //    {                    
            //        //Log($"{this.Name }task doing {Simulation}");
            //        //if (!Simulation) Colllect();
            //        Thread.Sleep(1000);
            //    }
            //});            
            //collectorTask.Start();

        }

        public void Report(string name)
        {

        }

        public void CheckMonitor()
        {
            rwl.AcquireReaderLock(1000);
            try
            {
                foreach (var item in Items)
                {
                    if (this.State == StartableState.StopInProgress || this.State == StartableState.Stopped) break;
                    decimal change = 0M;
                   
                    var oldVal = item.Value.OldValue;
                    var newValue = item.Value.CurrentValue;
                    if (!oldVal.HasValue) item.Value.OldValue = newValue;
                    else
                    {                        
                        if (newValue != oldVal)
                        {                            
                            var r = oldVal.Value != 0 ? ((newValue - oldVal.Value) / oldVal.Value) : 1;
                            change = 100M * r;
                            var f = Filters.Where(p => item.Value.Name.StartsWith(p.Filter)).FirstOrDefault();
                            var raise = f != null && Math.Abs(change) > f.ChangePercent; ;
                            if (raise)
                            {
                                if (MonitorEvent != null) MonitorEvent(this, new MonitorEventArgs() { Change = change, Item = item.Value, EventType = MonitorEventType.Updated });
                                item.Value.OldValue = newValue;
                            }
                        }
                    }
                }
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }

        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckMonitor();
        }

        public virtual void Stop()
        {
            this.State = StartableState.StopInProgress;
            try
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
                this.State = StartableState.Stopped;
            }
            finally
            {

            }
        }



        public bool RaiseEvent(MonitorEventArgs args)
        {
            var raise = true;
            if (Filters.Count > 0 && args.EventType == MonitorEventType.Updated)
            {
                var f = Filters.Where(p => args.Item.Name.StartsWith(p.Filter)).FirstOrDefault();
                raise = f != null && Math.Abs(args.Change) > f.ChangePercent;
            }
            if (raise && MonitorEvent != null) MonitorEvent(this, args);
            return raise;
        }

        public void Init(string name, decimal value)
        {
            var item = new MonitorItem() { Name = name, CurrentValue = value };
            AddItem(item);
            RaiseEvent(new MonitorEventArgs() { Item = item, EventType = MonitorEventType.Initialized });
        }

        private void AddItem(MonitorItem item)
        {
            rwl.AcquireWriterLock(1000);
            try
            {
                Items[item.Name] = item;
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
        }

        private IList<KeyValuePair<string, MonitorItem>> GetItems()
        {
            rwl.AcquireReaderLock(1000);
            try
            {
                return Items.ToList();
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }
        }

        private MonitorItem GetItem(string name)
        {
            rwl.AcquireReaderLock(1000);
            try
            {
                return Items[name];
            }
            finally
            {
                rwl.ReleaseReaderLock();
            }
        }

        public StringBuilder Dump(bool filtered)
        {
            var sb = new StringBuilder();
            var items = GetItems();
            var line = 0;
            foreach (var item in items)
            {
                var f = Filters.Where(p => item.Value.Name.StartsWith(p.Filter)).FirstOrDefault();
                if (!filtered || f != null)
                {
                    sb.Append($"{item.Key}:{item.Value.CurrentValue}\t");
                    if (++line % 2 == 0) sb.AppendLine("");
                }

            }
            return sb;
        }

        public void Set(string name, decimal value)
        {
            rwl.AcquireWriterLock(1000);
            try
            {
                var item = Items[name];
                //item.OldValue = item.CurrentValue;
                item.CurrentValue = value;
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }

            //var item = GetItem(name);
            //decimal change = 0;
            //if (item.CurrentValue != value)
            //{
            //    if (item.CurrentValue != 0) change = 100.0M * (value - item.CurrentValue) / item.CurrentValue;
            //    else change = 100;
            //    item.OldValue = item.CurrentValue;
            //    item.CurrentValue = value;
            //    RaiseEvent(new MonitorEventArgs() { Change = change, Item = item, EventType = MonitorEventType.Updated });

            //}
        }

    }
}
