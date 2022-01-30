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
using Matriks.Trader.Core.TraderModels;
using Matriks.Lean.Algotrader.AlgoBase;
using Matriks.Lean.Algotrader.Models;
using Matriks.Lean.Algotrader.Trading;


namespace Matriks.Lean.Algotrader
{
	public class testlimit : MatriksAlgo
	{
		// Strateji çalıştırılırken kullanacağımız parametreler. Eğer sembolle ilgili bir parametre ise,
		// "SymbolParameter" ile, değilse "Parameter" ile tanımlama yaparız. Parantez içindeki değerler default değerleridir.

		[SymbolParameter("GARAN")]
		public string Symbol;

		[Parameter(SymbolPeriod.Min5)]
		public SymbolPeriod SymbolPeriod;

		/// <summary>
		/// Strateji ilk çalıştırıldığında bu fonksiyon tetiklenir. Tüm sembole kayit işlemleri,
		/// indikator ekleme, haberlere kayıt olma işlemleri burada yapılır. 
		/// </summary>
		public override void OnInit()
		{
			AddSymbol(Symbol, SymbolPeriod);
			AddSymbolMarketData(Symbol);

			//Eger backtestte emri bir al bir sat seklinde gonderilmesi isteniyor bu true set edilir. 
			//Alttaki satırı silerek veya false geçerek emirlerin sirayla gönderilmesini engelleyebilirsiniz. 
			SendOrderSequential(true);
		}

		/// <summary>
		/// Init islemleri tamamlaninca, bardatalar kullanmaya hazir hale gelince bu fonksiyon tetiklenir. Data uzerinde bir defa yapilacak islemler icin kullanilir
		/// </summary>
		public override void OnInitCompleted()
		{
			var price =  GetMarketData(Symbol, SymbolUpdateField.Last);
			
			OrderSide side = OrderSide.Buy;

			decimal limitPrice = Math.Round((price + price * 0.02M * (side == OrderSide.Sell ? -1 : 1)) * 4, MidpointRounding.ToEven) / 4;


	

			Debug($"Market: {price} iletilen: {limitPrice}");
			string orderid  = this.SendLimitOrder(Symbol, 1, side, limitPrice, ChartIcon.None, DateTime.Now.Hour >= 19);
			Debug($"Emit iletildi. Emir no: {orderid}");
		}

		/// <summary>
		/// Eklenen sembollerin bardata'ları ve indikatorler güncellendikçe bu fonksiyon tetiklenir. 
		/// </summary>
		/// <param name="barData">Bardata ve hesaplanan gerçekleşen işleme ait detaylar</param>
		public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
		{

		}

		/// <summary>
		/// Gönderilen emirlerin son durumu değiştikçe bu fonksiyon tetiklenir.
		/// </summary>
		/// <param name="barData">Emrin son durumu</param>
		public override void OnOrderUpdate(IOrder order)
		{
			if (order.OrdStatus.Obj == OrdStatus.Filled)
			{

			}
		}

		/// <summary>
		/// Strateji durdurulduğunda bu fonksiyon tetiklenir.
		/// </summary>
		public override void OnStopped()
		{
		}
	}
}
