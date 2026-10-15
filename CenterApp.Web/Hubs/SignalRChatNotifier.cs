using CenterApp.Service.Models;
using CenterApp.Service.Services;
using Microsoft.AspNetCore.SignalR;

namespace CenterApp.Web.Hubs;

public class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<AppHub> _hub;
    public SignalRChatNotifier(IHubContext<AppHub> hub) => _hub = hub;

    public Task PushAsync(string userId, ChatLive live)
        => _hub.Clients.User(userId).SendAsync("incoming", live);
}
