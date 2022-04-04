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
using Newtonsoft.Json;

namespace Kalitte.Trading.Matrix
{
    public abstract class MatrixAlgoBase<T> : MatriksAlgo, IDisposable, IExchange where T : Kalitte.Trading.Algos.AlgoBase
    {

        VOLUME volume;

        public T Algo { get; set; }

        //[Parameter(1)]
        //public int LoggingLevel { get; set; } = 1;

        //[Parameter(false)]
        //public bool Simulation { get; set; } = false;



        public virtual FinanceBars GetPeriodBars(string symbol, BarPeriod period, DateTime t)
        {
            var mPeriod = (SymbolPeriod)Enum.Parse(typeof(SymbolPeriod), period.ToString());
            Algo.Log($"getperiod {symbol} {period} {mPeriod}", LogLevel.Error);
            var periodBars = new FinanceBars(symbol, period);
            var bd = GetBarData(symbol, mPeriod);
            if (bd != null && bd.BarDataIndexer != null)
            {
                for (var i = 0; i < bd.BarDataIndexer.LastBarIndex; i++)
                {
                    if (bd.BarDataIndexer[i] > t) break;
                    var quote = new MyQuote() { Date = bd.BarDataIndexer[i], Open = bd.Open[i], High = bd.High[i], Low = bd.Low[i], Close = bd.Close[i], Volume = bd.Volume[i] };
                    periodBars.Push(quote);
                }
            }
            else return null;
            return periodBars;
        }

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
                    Algo.FillCurrentOrder((order.FilledAmount / order.FilledQty) / 10M, order.FilledQty);
                }
            }
            else if (order.OrdStatus.Obj == OrdStatus.Rejected || order.OrdStatus.Obj == OrdStatus.Canceled)
            {
                if (Algo.positionRequest != null && Algo.positionRequest.Id == order.CliOrdID)
                {
                    Algo.CancelCurrentOrder(order.OrdStatus.Obj.ToString());
                }
            } else if (Algo.positionRequest != null && Algo.positionRequest.Id == order.CliOrdID)
            {
                Algo.positionRequest.LastUpdate = Algo.Now;
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


        public override void OnInitComplated()
        {
            var config = Algo.GetConfigValues();
            foreach (var item in config)
            {
                Debug($"{item.Key}:{item.Value}");
            }
            if (!Algo.Simulation)
            {
                var assembly = typeof(MatrixBist30).Assembly.GetName();
                Algo.Log($"{this}", LogLevel.Info);
                if (Algo.UseVirtualOrders)
                {
                    Algo.Log($"Using ---- VIRTUAL ORDERS ----", LogLevel.Warning);
                }
                var portfolioItems = LoadRealPositions(GetRealPositions(), p => p.Symbol == Algo.Symbol);
                Algo.InitializePositions(portfolioItems);
                Algo.InitializeBars(Algo.Symbol, Algo.SymbolPeriod);
                Algo.Start();
                Algo.Log($"- STARTED -");
                if (Algo.UserPortfolioList.Count > 0) Algo.Log($"{Algo.UserPortfolioList.Print()}");
                else Algo.Log("!! Portfolio is empty !!");

            }
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



        internal List<PortfolioItem> LoadRealPositions(Dictionary<string, AlgoTraderPosition> positions, Func<AlgoTraderPosition, bool> filter)
        {
           var list = new List<PortfolioItem>();
            //Algo.UserPortfolioList.Clear();
            foreach (var position in positions)
            {
                if (position.Value.IsSymbol)
                {
                    if (filter(position.Value))
                        list.Add(FromTraderPosition(position.Value));
                }
            }
            return list;
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



        public decimal GetVolume(string symbol, BarPeriod period, DateTime? t = null)
        {
            return volume.CurrentValue;
        }

        public virtual decimal GetMarketPrice(string symbol, DateTime? t = null)
        {
            try
            {
                return this.GetMarketData(symbol, SymbolUpdateField.Last);
            } catch (Exception ex)
            {
                Algo.Log($"{ex.Message}", LogLevel.Error);
                return 0;
            }
            

        }

    }

}
