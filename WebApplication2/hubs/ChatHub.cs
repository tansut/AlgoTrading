using Kalitte.Trading;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            var s = new List<string>(message.Split(',')).Select(x => decimal.Parse(x)).ToArray();

            TopQue q = new TopQue(s.Length);


           
            foreach (var n in s)
            {           
                q.Push(n);
            };

            var res = new List<decimal>();
            for (var i = 1; i < s.Length + 1; i++)
            {


                res.Add(q.CalcEma(i).Last());


            }


 
            await Clients.All.SendAsync("ReceiveMessage", user, String.Join(",", res.Select(p => p.ToString()).ToArray()));
        }
    }
}