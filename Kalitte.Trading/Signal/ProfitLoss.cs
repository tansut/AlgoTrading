// algo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
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

    public class ProfitLossResult: SignalResult
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
        public decimal UsedProfitPriceChange { get; set; }
        public decimal UsedLossPriceChange { get; set; }


        public decimal ProfitPriceChange { get; set; }
        public decimal ProfitQuantity { get; set; }
        public decimal LossPriceChange { get; set; }
        public decimal LossQuantity { get; set; }

        public int SignalCount { get; set; }

        public TakeProfitOrLossSignal(string name, string symbol, AlgoBase owner, 
            decimal profitPriceChange, decimal profitQuantity, decimal lossPriceChange, decimal lossQuantity) : base(name, symbol, owner)
        {
            ProfitPriceChange = profitPriceChange;
            ProfitQuantity = profitQuantity;
            LossPriceChange =lossPriceChange;
            LossQuantity = lossQuantity;
            UsedProfitPriceChange = profitPriceChange;
            UsedLossPriceChange = lossPriceChange;
            SignalCount = 0;
        }

        public void ResetPriceChange()
        {
            UsedProfitPriceChange = ProfitPriceChange;
            UsedLossPriceChange = LossPriceChange;
            SignalCount = 0;
        }

        public void AdjustPriceChange(decimal ratio)
        {
            UsedProfitPriceChange = ratio * ProfitPriceChange;
            UsedLossPriceChange = ratio * LossPriceChange;
        }


        public override string ToString()
        {
            return $"{base.ToString()}: profit:{ProfitQuantity}/{ProfitPriceChange} loss: {LossQuantity}/{LossPriceChange}.";
        }

        protected override void ResetInternal()
        {
            ResetPriceChange();
        }


        protected override SignalResult CheckInternal(DateTime? t = null)
        {

            BuySell? result = null;
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
                else if (ProfitQuantity > 0 && portfolio.Side == BuySell.Buy && pl >= this.UsedProfitPriceChange)
                {
                    direction = ProfitOrLoss.Profit;
                    result = BuySell.Sell;
                    
                }
                else if (ProfitQuantity > 0 && portfolio.Side == BuySell.Sell && -pl >= this.UsedProfitPriceChange)
                {
                    direction = ProfitOrLoss.Profit;
                    result = BuySell.Buy;
                }
                else if (LossQuantity > 0 && portfolio.Side == BuySell.Buy && pl <= -this.UsedLossPriceChange)
                {
                    direction = ProfitOrLoss.Loss;
                    result = BuySell.Sell;
                }
                else if (LossQuantity > 0 && portfolio.Side == BuySell.Sell && pl >= this.UsedLossPriceChange)
                {
                    direction = ProfitOrLoss.Loss;
                    result = BuySell.Buy;
                }
                //else Algo.Log($"No cation takeprofit: PL: {pl}, price: {price}, cost: {portfolio.AvgCost}", LogLevel.Debug);
            }
            if (result.HasValue) SignalCount++;
            return new ProfitLossResult(this, t ?? DateTime.Now) { Direction= direction, PL=pl, MarketPrice=price,PortfolioCost=avgCost, finalResult = result };


        }

    }



}
