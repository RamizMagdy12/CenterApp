using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CenterApp.Web.Hubs;

[Authorize]
public class AppHub : Hub
{
}
