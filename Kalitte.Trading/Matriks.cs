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


namespace Matriks.Lean.Algotrader
{
    public class MaAlg : Kalitte.Trading.Algos.MaProfit
    {

    }

    public class backtest : Kalitte.Trading.Algos.MaProfit
    {
        public override void OnInit()
        {
            //this.Simulation = true;
            base.OnInit();
        }
    }

    public class testalgo : Kalitte.Trading.Algos.MaProfit
    {
        public override void OnInit()
        {

            base.OnInit();
        }
    }

    public class log : Kalitte.Trading.Algos.PriceLogger
    {

    }

    public class bardatalogger : Kalitte.Trading.Algos.BarDataLogger
    {

    }
}
