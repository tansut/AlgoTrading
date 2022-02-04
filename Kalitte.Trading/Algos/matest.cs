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
using System.IO;
using System.Threading.Tasks;

namespace Matriks.Lean.Algotrader
{
	public class matest : MatriksAlgo
	{
		// Strateji çalıştırılırken kullanacağımız parametreler. Eğer sembolle ilgili bir parametre ise,
		// "SymbolParameter" ile, değilse "Parameter" ile tanımlama yaparız. Parantez içindeki değerler default değerleridir.

		[SymbolParameter("F_XU0300222")]
		public string Symbol;

		[Parameter(SymbolPeriod.Min10)]
		public SymbolPeriod SymbolPeriod;

		[Parameter(1)]
		public decimal BuyOrderQuantity;

		[Parameter(1)]
		public decimal SellOrderQuantity;


		 

		// indikator tanımları.

		MOV mov;
		MOV mov2;
		RSI rsi;
		MACD macd;

		/// <summary>
		/// Strateji ilk çalıştırıldığında bu fonksiyon tetiklenir. Tüm sembole kayit işlemleri,
		/// indikator ekleme, haberlere kayıt olma işlemleri burada yapılır. 
		/// </summary>
		public override void OnInit()
		{
			AddSymbol(Symbol, SymbolPeriod);


			mov = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, 9, MovMethod.Exponential);
			mov2 = MOVIndicator(Symbol, SymbolPeriod, OHLCType.Close, 5, MovMethod.Exponential);
			rsi = RSIIndicator(Symbol, SymbolPeriod, OHLCType.Close, 14);
			macd = MACDIndicator(Symbol, SymbolPeriod, OHLCType.Close, 9, 5, 3);

			// Algoritmanın kalıcı veya geçici sinyal ile çalışıp çalışmayacağını belirleyen fonksiyondur.
			// true geçerseniz algoritma sadece yeni bar açılışlarında çalışır, bu fonksiyonu çağırmazsanız veya false geçerseniz her işlem olduğunda algoritma tetiklenir.
			WorkWithPermanentSignal(true);

			//Eger emri bir al bir sat seklinde gonderilmesi isteniyor bu true set edilir. 
			//Alttaki satırı silerek veya false geçerek emirlerin sirayla gönderilmesini engelleyebilirsiniz. 
			SendOrderSequential(true);
			//Alttaki fonksiyon açıldıktan sonra parametre olarak verilen saniyede bir OnTimer fonksiyonu tetiklenir.
			//SetTimerInterval(3);

			//Alttaki fonksiyon ile tanımlanan sembol ile ilgili haber geldiğinde OnNewsReceived fonksiyonu tetiklenir.
			//AddNewsSymbol(Symbol);

			//Alttaki fonksiyon ile tanımlanan anahtar kelime ile ilgili haber geldiğinde OnNewsReceived fonksiyonu tetiklenir.
			//AddNewsKeyword("KAP");
		}

		/// <summary>
		/// Init islemleri tamamlaninca, bardatalar kullanmaya hazir hale gelince bu fonksiyon tetiklenir. Data uzerinde bir defa yapilacak islemler icin kullanilir
		/// </summary>
		public override void OnInitCompleted()
		{
			

		}

		/// <summary>
		/// SetTimerInterval fonksiyonu ile belirtilen sürede bir bu fonksiyon tetiklenir.
		/// </summary>
		public override void OnTimer()
		{

		}

		/// <summary>
		/// AddNewsSymbol ve AddNewsKeyword ile haberlere kayit olunmuşsa bu fonksiyon tetiklenir.
		/// </summary>
		/// <param name="newsId">Gelen haberin id'si</param>
		/// <param name="relatedSymbols">Gelen haberin ilişkili sembolleri</param>
		public override void OnNewsReceived(int newsId, List<string> relatedSymbols)
		{

		}

		/// <summary>
		/// Eklenen sembollerin bardata'ları ve indikatorler güncellendikçe bu fonksiyon tetiklenir. 
		/// </summary>
		/// <param name="barData">Bardata ve hesaplanan gerçekleşen işleme ait detaylar</param>
		/// 

		private void saveBardataValue(BarDataValue bd, List<string> list)
        {
			list.Add($"t: {bd.DTime} o:{bd.Open} h:{bd.High} l:{bd.Low} c:{bd.Close} wc:{bd.WClose} dif:{bd.Diff} dif%:{bd.DiffPercent} vol:{bd.Volume}");
			list.Add($"t: {bd.DTime} rsi: {rsi.CurrentValue} ma5: {mov.CurrentValue} ma9: {mov2.CurrentValue} macd: {macd.CurrentValue} {macd.MacdTrigger.CurrentValue}");
			list.Add($"t: {bd.DTime} macb: {CrossBelow(mov, mov2)} maca: {CrossAbove(mov, mov2)} macdcb: {CrossBelow(macd, macd.MacdTrigger)} macdcb: {CrossAbove(macd, macd.MacdTrigger)}");
		}
		public override void OnDataUpdate(BarDataCurrentValues barDataCurrentValues)
		{
			//File.AppendAllLines(@"c:\kalitte\log\0203period.txt", )
			
			var list = new List<string>();
			foreach(var bd in barDataCurrentValues.BarDataValues)
            {				
				saveBardataValue(bd, list);
			}

			list.Add("--CURRENT-");
			saveBardataValue(barDataCurrentValues.LastUpdate, list);
			
			foreach(var line in list)
            {
				Debug(line);
            }
			File.AppendAllLines(@"c:\kalitte\log\0203period10min.txt", list);

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
