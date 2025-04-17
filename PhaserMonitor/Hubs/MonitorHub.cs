using Microsoft.AspNetCore.SignalR;
using PhaserMonitor.Services;

namespace PhaserMonitor.Hubs
{
    public class MonitorHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Notify", "Conectado ao SignalR!");
            await base.OnConnectedAsync();
        }
    }
}
