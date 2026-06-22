using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Service.Hubs
{
    public class NewsHub : Hub
    {
        public async Task NotifyNewsChange()
        {
            await Clients.All.SendAsync("UpdateNews");
        }
    }
}
