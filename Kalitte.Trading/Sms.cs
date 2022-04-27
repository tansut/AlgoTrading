using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Kalitte.Trading
{
    public static class Sms
    {
        static readonly HttpClient client = new HttpClient();
        static readonly Dictionary<string, string> IdList = new Dictionary<string, string>();

        public static void SendSms(string to, string text, string id = "")
        {
            if (!string.IsNullOrEmpty(id) && IdList.ContainsKey(id)) return;
            var url = $"https://api.netgsm.com.tr/sms/send/get?usercode=8503054216&password=BOV0MN1M&gsmno={HttpUtility.UrlEncode(to)}&message={HttpUtility.UrlEncode(text)}&msgheader=KALITTE LTD";
            var task = Task.Run(() => client.GetAsync(url));
            task.Wait();
            var response = task.Result;
            var task2 = Task.Run(() => response.Content.ReadAsStringAsync());
            task2.Wait();
            var result = task2.Result;
            var codes = new string[] { "20", "30", "40", "70" };
            if (codes.Any(s => s == result)) throw new Exception($"Unable to send SMS {result}");
            if (!string.IsNullOrEmpty(id)) IdList.Add(id, to);
        }
    }
}

