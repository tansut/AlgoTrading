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

namespace Kalitte.Trading
{
    public enum ProfitOrLoss
    {
        Loss,
        Profit
    }

    public class ProfitLossResult: SignalResultX
    {
        public decimal PL { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal PortfolioCost { get; set; }
        public ProfitOrLoss Direction { get; set; }

        public ProfitLossResult(Signal signal, DateTime t): base(signal, t)
        {

        }
    }

    public class TakeProfitOrLossSignal : Signal
    {
        public decimal ProfitPriceChange { get; set; }
        public decimal ProfitQuantity { get; set; }
        public decimal LossPriceChange { get; set; }
        public decimal LossQuantity { get; set; }

        public TakeProfitOrLossSignal(string name, string symbol, AlgoBase owner, 
            decimal profitPriceChange, decimal profitQuantity, decimal lossPriceChange, decimal lossQuantity) : base(name, symbol, owner)
        {
            ProfitPriceChange = profitPriceChange;
            ProfitQuantity = profitQuantity;
            LossPriceChange =lossPriceChange;
            LossQuantity = lossQuantity;

        }


        public override string ToString()
        {
            return $"{base.ToString()}: profit:{ProfitQuantity}/{ProfitPriceChange} loss: {LossQuantity}/{LossPriceChange}.";
        }


        protected override SignalResultX CheckInternal(DateTime? t = null)
        {

            OrderSide? result = null;
            decimal price = 0M;
            decimal pl = 0M;
            decimal avgCost = 0M;
            
            var portfolio = Algo.UserPortfolioList.GetPortfolio(this.Symbol);
            var direction = ProfitOrLoss.Profit;

            if (!portfolio.IsEmpty)
            {
                price = Algo.GetMarketPrice(Symbol, t);                   
                avgCost = portfolio.AvgCost;
                pl = price - avgCost;
               
                if (price == 0 || avgCost == 0)
                {
                    //Log($"ProfitLoss/Portfolio Cost price is zero: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Verbose, t);
                }
                else if (ProfitQuantity > 0 && portfolio.Side == OrderSide.Buy && pl >= this.ProfitPriceChange)
                {
                    direction = ProfitOrLoss.Profit;
                    result = OrderSide.Sell;
                }
                else if (ProfitQuantity > 0 && portfolio.Side == OrderSide.Sell && -pl >= this.ProfitPriceChange)
                {
                    direction = ProfitOrLoss.Profit;
                    result = OrderSide.Buy;
                }
                else if (LossQuantity > 0 && portfolio.Side == OrderSide.Buy && pl <= -this.LossPriceChange)
                {
                    direction = ProfitOrLoss.Loss;
                    result = OrderSide.Sell;
                }
                else if (LossQuantity > 0 && portfolio.Side == OrderSide.Sell && pl >= this.LossPriceChange)
                {
                    direction = ProfitOrLoss.Loss;
                    result = OrderSide.Buy;
                }
                //else Algo.Log($"No cation takeprofit: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
            }

            return new ProfitLossResult(this, t ?? DateTime.Now) { Direction= direction, PL=pl, MarketPrice=price,PortfolioCost=avgCost, finalResult = result };


        }

    }



}
