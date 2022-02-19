// algo
using System;
using System.Collections.Generic;
using System.Linq;
using Matriks.Data.Symbol;
using Matriks.Engines;
using Matriks.Indicators;
using Matriks.Symbols;
using Matriks.AlgoTrader;
using Matriks.Trader.Core;
using Matriks.Trader.Core.Fields;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;
using System.Timers;
using Matriks.Trader.Core.TraderModels;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kalitte.Trading.Algos;

namespace Kalitte.Trading.Matrix
{
    public abstract class MatrixAlgoBase<T> : MatriksAlgo, IDisposable, IMarketDataProvider, ILogProvider where T: Kalitte.Trading.Algos.AlgoBase
    {

        //public static MatrixAlgoBase<T> Current;

        public T Algo { get; set; }

        [Parameter(1)]
        public int LoggingLevel { get; set; } = 1;

        [Parameter(false)]
        public bool Simulation = false;


        
 

        protected abstract T createAlgoInstance();

        public MatrixAlgoBase()
        {
            this.Algo = createAlgoInstance();
            Algo.DataProvider = this;
        }


        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            Algo.Log(text, level, t);
            Debug(text);
        }

        public override void OnInit()
        {
            Algo.Simulation = this.Simulation;
            Algo.LoggingLevel = (LogLevel)this.LoggingLevel;
            Algo.Init();
        }


        public override void OnStopped()
        {
            Algo.Stop();
            base.OnStopped();
        }



        public List<Signal> Signals = new List<Signal>();
        public ConcurrentDictionary<string, SignalResultX> SignalResults = new ConcurrentDictionary<string, SignalResultX>();

        internal void LoadRealPositions(Dictionary<string, AlgoTraderPosition> positions, Func<AlgoTraderPosition, bool> filter)
        {
            UserPortfolioList.Clear();
            foreach (var position in positions)
            {
                if (position.Value.IsSymbol)
                {
                    if (filter(position.Value))
                        UserPortfolioList.Add(position.Key, FromTraderPosition(position.Value));
                }
            }
        }

        public PortfolioItem FromTraderPosition(AlgoTraderPosition p)
        {
            var item = new PortfolioItem(p.Symbol);
            LoadFromTraderPosition(item, p);
            return item;
        }

        public void LoadFromTraderPosition(PortfolioItem item, AlgoTraderPosition p)
        {

            item.Symbol = p.Symbol;
            item.Side = p.Side.Obj == Matriks.Trader.Core.Fields.Side.Buy ? OrderSide.Buy : OrderSide.Sell;
            item.AvgCost = p.AvgCost;
            item.Quantity = Math.Abs(p.QtyNet);
        }

        public PortfolioItem UpdateFromTrade(AlgoTraderPosition position)
        {
            var item = UserPortfolioList.GetPortfolio(position.Symbol);
            LoadFromTraderPosition(item, position);
            return item;
        }

        public void LoadRealPositions(string symbol)
        {
            var positions = Simulation ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            LoadRealPositions(positions, p => p.Symbol == symbol);
            Log($"- PORTFOLIO -");
            if (UserPortfolioList.Count > 0) Log($"{UserPortfolioList.Print()}");
            else Log("!! Portfolio is empty !!");
            Log($"- END PORTFOLIO -");
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            return this.GetMarketData(symbol, SymbolUpdateField.Last);

        }

    }

}
