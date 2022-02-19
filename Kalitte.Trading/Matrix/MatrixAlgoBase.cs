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
    public abstract class MatrixAlgoBase<T> : MatriksAlgo, IDisposable, IExchange, ILogProvider where T: Kalitte.Trading.Algos.AlgoBase
    {

        //public static MatrixAlgoBase<T> Current;

        public T Algo { get; set; }

        [Parameter(1)]
        public int LoggingLevel { get; set; } = 1;

        [Parameter(false)]
        public bool Simulation { get; set; } = false;



        public string CreateMarketOrder(string symbol, decimal quantity, BuySell side, string icon, bool night)
        {
            return this.SendMarketOrder(symbol, quantity, side == BuySell.Buy ? OrderSide.Buy: OrderSide.Sell, (ChartIcon)Enum.Parse(typeof(ChartIcon), icon), night);
        }

        public string CreateLimitOrder(string symbol, decimal quantity, BuySell side, decimal limitPrice, string icon, bool night)
        {
            return this.SendLimitOrder(symbol, quantity, side == BuySell.Buy ? OrderSide.Buy : OrderSide.Sell, limitPrice, (ChartIcon)Enum.Parse(typeof(ChartIcon), icon), night);

        }



        public override void OnOrderUpdate(IOrder order)
        {
            Log($"OrderUpdate: status: {order.OrdStatus.Obj} cliD: {order.CliOrdID} oid: {order.OrderID} algoid: {order.AlgoId} fa: {order.FilledAmount}", LogLevel.Debug);
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {
                //if (!BackTestMode) Log($"OrderUpdate: pos: {this.positionRequest} status: {order.OrdStatus.Obj} orderid: {order.CliOrdID} fa: {order.FilledAmount} fq: {order.FilledQty} price: {order.Price} lastx: {order.LastPx}", LogLevel.Debug, this.positionRequest != null ? this.positionRequest.Time: DateTime.Now);

                if (Algo.positionRequest != null && Algo.positionRequest.Id == order.CliOrdID)
                {
                    Algo.positionRequest.Resulted = order.TradeDate;
                    if (Simulation)
                    {
                        if (Algo.positionRequest.UnitPrice > 0 && Algo.positionRequest.UnitPrice != order.Price)
                        {
                            var gain = ((order.Price - Algo.positionRequest.UnitPrice) * (Algo.positionRequest.Side == BuySell.Buy ? 1 : -1) * Algo.positionRequest.Quantity);
                            Algo.simulationPriceDif += gain;
                            Log($"Filled price difference for order {order.CliOrdID}: Potential: {Algo.positionRequest.UnitPrice}, Backtest: {order.Price} Difference: [{gain}]", LogLevel.Warning, Algo.positionRequest.Resulted);
                            //this.FillCurrentOrder(positionRequest.UnitPrice, this.positionRequest.Quantity);
                        }
                        Algo.FillCurrentOrder(order.Price, Algo.positionRequest.Quantity);
                    }
                    else
                    {
                        Algo.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                    }
                }
            }
            else if (order.OrdStatus.Obj == OrdStatus.Rejected || order.OrdStatus.Obj == OrdStatus.Canceled)
            {
                if (Algo.positionRequest != null && Algo.positionRequest.Id == order.CliOrdID)
                {
                    Algo.CancelCurrentOrder(order.OrdStatus.Obj.ToString());
                }
            }
        }



        protected abstract T createAlgoInstance();

        public MatrixAlgoBase()
        {
            this.Algo = createAlgoInstance();
            Algo.Exchange = this;
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

        public void SetAlgoProperties()
        {
            var properties = this.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(ParameterAttribute), true));
            foreach (var item in properties)
            {
                Algo.GetType().GetProperty(item.Name).SetValue(Algo, item.GetValue(this));
            }
        }


        public List<Signal> Signals = new List<Signal>();
        public ConcurrentDictionary<string, SignalResultX> SignalResults = new ConcurrentDictionary<string, SignalResultX>();

        internal void LoadRealPositions(Dictionary<string, AlgoTraderPosition> positions, Func<AlgoTraderPosition, bool> filter)
        {
            Algo.UserPortfolioList.Clear();
            foreach (var position in positions)
            {
                if (position.Value.IsSymbol)
                {
                    if (filter(position.Value))
                        Algo.UserPortfolioList.Add(position.Key, FromTraderPosition(position.Value));
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
            item.Side = p.Side.Obj == Matriks.Trader.Core.Fields.Side.Buy ? BuySell.Buy : BuySell.Sell;
            item.AvgCost = p.AvgCost;
            item.Quantity = Math.Abs(p.QtyNet);
        }

        public PortfolioItem UpdateFromTrade(AlgoTraderPosition position)
        {
            var item = Algo.UserPortfolioList.GetPortfolio(position.Symbol);
            LoadFromTraderPosition(item, position);
            return item;
        }

        public void LoadRealPositions(string symbol)
        {
            var positions = Simulation ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            LoadRealPositions(positions, p => p.Symbol == symbol);
            Log($"- PORTFOLIO -");
            if (Algo.UserPortfolioList.Count > 0) Log($"{Algo.UserPortfolioList.Print()}");
            else Log("!! Portfolio is empty !!");
            Log($"- END PORTFOLIO -");
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            return this.GetMarketData(symbol, SymbolUpdateField.Last);

        }

    }

}
