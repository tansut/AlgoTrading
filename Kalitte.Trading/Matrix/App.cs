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
    public class MaAlg : Kalitte.Trading.Matrix.MaProfit
    {

    }

    public class MaAlg2 : Kalitte.Trading.Matrix.MaProfit
    {

    }

    public class MaAlgMacd : Kalitte.Trading.Matrix.MaProfit
    {

    }

    

    public class backtest : Kalitte.Trading.Matrix.MaProfit
    {
        public override void OnInit()
        {
            
            base.OnInit();
        }
    }

    public class testalgo : Kalitte.Trading.Matrix.MaProfit
    {
        public override void OnInit()
        {

            base.OnInit();
        }
    }



    //public class bardatalogger : Kalitte.Trading.Matrix.BarDataLogger
    //{

    //}
}
