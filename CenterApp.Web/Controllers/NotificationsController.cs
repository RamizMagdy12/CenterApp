using CenterApp.Service.Models;
using CenterApp.Service.Services;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class NotificationsController : AppController
{
    private readonly IChatService _chat;
    public NotificationsController(IChatService chat) => _chat = chat;

    public async Task<IActionResult> Recent() => Json(await _chat.BellAsync(CurrentUserId));

    [HttpPost]
    public async Task<IActionResult> Read(long? conversationId)
    {
        await _chat.MarkReadAsync(CurrentUserId, conversationId);
        return Json(OpResult.Success());
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
        => Json(await _chat.ClearNotificationsAsync(CurrentUserId, IsAdmin));
}
