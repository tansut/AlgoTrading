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
    public abstract class MatrixAlgoBase<T> : MatriksAlgo, IDisposable, IExchange where T : Kalitte.Trading.Algos.AlgoBase
    {

        VOLUME volume;

        public T Algo { get; set; }

        [Parameter(1)]
        public int LoggingLevel { get; set; } = 1;

        [Parameter(false)]
        public bool Simulation { get; set; } = false;



        public string CreateMarketOrder(string symbol, decimal quantity, BuySell side, string icon, bool night)
        {
            return this.SendMarketOrder(symbol, quantity, side == BuySell.Buy ? OrderSide.Buy : OrderSide.Sell, (ChartIcon)Enum.Parse(typeof(ChartIcon), icon), night);
        }

        public string CreateLimitOrder(string symbol, decimal quantity, BuySell side, decimal limitPrice, string icon, bool night)
        {
            return this.SendLimitOrder(symbol, quantity, side == BuySell.Buy ? OrderSide.Buy : OrderSide.Sell, limitPrice, (ChartIcon)Enum.Parse(typeof(ChartIcon), icon), night);

        }



        public override void OnOrderUpdate(IOrder order)
        {
            Algo.Log($"OrderUpdate: status: {order.OrdStatus.Obj} cliD: {order.CliOrdID} oid: {order.OrderID} algoid: {order.AlgoId} fa: {order.FilledAmount}", LogLevel.Debug);
            if (order.OrdStatus.Obj == OrdStatus.Filled)
            {                

                if (Algo.positionRequest != null && Algo.positionRequest.Id == order.CliOrdID)
                {
                    Algo.positionRequest.Resulted = order.TradeDate;
                    if (Simulation)
                    {
                        if (Algo.positionRequest.UnitPrice > 0 && Algo.positionRequest.UnitPrice != order.Price)
                        {
                            var gain = ((order.Price - Algo.positionRequest.UnitPrice) * (Algo.positionRequest.Side == BuySell.Buy ? 1 : -1) * Algo.positionRequest.Quantity);
                            Algo.simulationPriceDif += gain;
                            //Algo.Log($"Filled price difference for order {order.CliOrdID}: Potential: {Algo.positionRequest.UnitPrice}, Backtest: {order.Price} Difference: [{gain}]", LogLevel.Warning, Algo.positionRequest.Resulted);
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

        public override void OnInit()
        {            
            Algo.Init();
            volume = VolumeIndicator(Algo.Symbol, (SymbolPeriod)Enum.Parse(typeof(SymbolPeriod), Algo.SymbolPeriod.ToString()));
            
        }

        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            Debug(text);
        }

        public override void OnStopped()
        {
            Algo.Stop();
            base.OnStopped();
        }

        public void SetAlgoProperties()
        {
            var properties = this.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(ParameterAttribute), true));
            Debug($"Setting Algo Properties({properties.Count()})");
            foreach (var item in properties)
            {
                Debug($"{item.Name} -> {item.GetValue(this)}");
                Algo.GetType().GetProperty(item.Name).SetValue(Algo, item.GetValue(this));
            }
        }



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

        public override string ToString()
        {
            return Algo.ToString();
        }

        public void LoadRealPositions(string symbol)
        {
            var positions = Simulation ? new Dictionary<string, AlgoTraderPosition>() : GetRealPositions();
            LoadRealPositions(positions, p => p.Symbol == symbol);
            if (!Simulation)
            {
                Algo.Log($"- PORTFOLIO -");
                if (Algo.UserPortfolioList.Count > 0) Algo.Log($"{Algo.UserPortfolioList.Print()}");
                else Algo.Log("!! Portfolio is empty !!");
                Algo.Log($"- END PORTFOLIO -");
            }            
        }

        public decimal GetVolume(string symbol, BarPeriod period, DateTime? t = null)
        {
            return volume.CurrentValue;
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            return this.GetMarketData(symbol, SymbolUpdateField.Last);

        }

    }

}
