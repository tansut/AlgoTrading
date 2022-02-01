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

namespace Kalitte.Trading
{
    //public class TakeProfitSignal : Signal
    //{        
    //    public decimal PriceChange { get; set; }
    //    public decimal Quantity { get; set; }

    //    public TakeProfitSignal(string name, Kalitte.Trading.Algos.KalitteAlgo owner, bool enabled, decimal priceChange, decimal quantity) : base(name, owner, enabled)
    //    {
    //        PriceChange = priceChange;
    //        quantity = Quantity;
    //    }


    //    public override SignalResultX Check(DateTime? t = null)
    //    {
    //        var portfolio = Owner.portfolios.GetPortfolio(Owner.Symbol);

    //        if (!portfolio.IsEmpty)
    //        {
    //            var price = marketPrice.HasValue ? marketPrice.Value : GetMarketPrice(t);
    //            var pl = price - portfolio.AvgCost;

    //            if (price == 0)
    //            {
    //                Log($"ProfitLoss price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
    //            }
    //            else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Buy) && (pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
    //            {
    //                takeProfitTotal += Math.Abs(pl);
    //                Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
    //                sendOrder(Symbol, ProfitQuantity, OrderSide.Sell, $"take profit order, PL: {pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
    //                result = OrderSide.Sell;
    //            }
    //            else if ((this.ProfitQuantity > 0) && (portfolio.Side == OrderSide.Sell) && (-pl >= this.ProfitPuan) && (portfolio.Quantity == this.OrderQuantity))
    //            {
    //                takeProfitTotal += Math.Abs(pl);
    //                Log($"TakeProfit: t:{t ?? DateTime.Now} PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
    //                sendOrder(Symbol, ProfitQuantity, OrderSide.Buy, $"take profit order, PL: {-pl}, totalTakeProfit: {takeProfitTotal}", price, ChartIcon.TakeProfit);
    //                result = OrderSide.Buy;
    //            }
    //        }


    //    }

    //}


}
