using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace SignalRChat
{

    [HubName("chathub")]
    public class ChatHub : Hub
    {


        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public void updateprice(string symbol, decimal price)
        {
            

            Clients.All.stockpriceupdate(symbol, price);
            Console.WriteLine(symbol, price);
        }


        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            //Clients.All.stockpriceupdate(name, message);
        }
    }
}