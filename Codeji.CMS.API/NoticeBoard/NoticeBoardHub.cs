using Codeji.CMS.API.Notification;
using Microsoft.AspNetCore.SignalR;

public class NoticeBoardHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}