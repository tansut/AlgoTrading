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
    public class ProfitLossResult: SignalResultX
    {
        public decimal PL { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal PortfolioCost { get; set; }

        public ProfitLossResult(Signal signal): base(signal)
        {

        }
    }

    public class TakeProfitSignal : Signal
    {
        public decimal PriceChange { get; set; }
        public decimal Quantity { get; set; }

        public TakeProfitSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, decimal priceChange, decimal quantity) : base(name, symbol, owner)
        {
            PriceChange = priceChange;
            Quantity = quantity;            
        }


        public override SignalResultX Check(DateTime? t = null)
        {

            OrderSide? result = null;
            decimal price = 0M;
            decimal pl = 0M;
            decimal avgCost = 0M;
            
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);

            if (!portfolio.IsEmpty)
            {
                price = Algo.GetMarketPrice(Symbol, t);                   
                avgCost = portfolio.AvgCost;
                pl = price - avgCost;

                if (price == 0 || avgCost == 0)
                {
                    Algo.Log($"ProfitLoss/Portfolio Cost price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                }
                else if (portfolio.Side == OrderSide.Buy && pl >= this.PriceChange)
                {                   
                    result = OrderSide.Sell;
                }
                else if (portfolio.Side == OrderSide.Sell && -pl >= this.PriceChange)
                {                    
                    result = OrderSide.Buy;
                }
            }

            return new ProfitLossResult(this) { PL=pl, MarketPrice=price,PortfolioCost=avgCost, finalResult = result };


        }

    }

    public class StopLossSignal : Signal
    {
        public decimal PriceChange { get; set; }
        public decimal Quantity { get; set; }

        public StopLossSignal(string name, string symbol, Kalitte.Trading.Algos.AlgoBase owner, decimal priceChange, decimal quantity) : base(name, symbol, owner)
        {
            PriceChange = priceChange;
            Quantity = quantity;
        }


        public override SignalResultX Check(DateTime? t = null)
        {

            OrderSide? result = null;
            decimal price = 0M;
            decimal pl = 0M;
            decimal avgCost = 0M;

            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);

            if (!portfolio.IsEmpty)
            {
                price = Algo.GetMarketPrice(Symbol, t);
                avgCost = portfolio.AvgCost;
                pl = price - avgCost;

                if (price == 0 || avgCost == 0)
                {
                    Algo.Log($"ProfitLoss/Portfolio Cost price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
                }
                else if (portfolio.Side == OrderSide.Buy && pl <= -this.PriceChange)
                {
                    result = OrderSide.Sell;
                }
                else if (portfolio.Side == OrderSide.Sell && pl >= this.PriceChange)
                {
                    result = OrderSide.Buy;
                }
            }

            return new ProfitLossResult(this) { PL = pl, MarketPrice = price, PortfolioCost = avgCost, finalResult = result };


        }

    }


}
