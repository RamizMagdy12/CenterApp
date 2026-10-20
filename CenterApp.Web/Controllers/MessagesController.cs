using CenterApp.Domain.Security;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Messages, PermissionAction.View)]
public class MessagesController : AppController
{
    private readonly IChatService _chat;
    private readonly IChatNotifier _hub;
    public MessagesController(IChatService chat, IChatNotifier hub) { _chat = chat; _hub = hub; }

    public async Task<IActionResult> Index()
    {
        if (!IsAdmin && await _chat.TeacherIdOfUserAsync(CurrentUserId) == null)
        {
            ViewBag.Unlinked = true;
            return View(new List<ConversationRow>());
        }
        var scope = IsAdmin ? null : await _chat.TeacherIdOfUserAsync(CurrentUserId);
        return View(await _chat.InboxAsync(scope));
    }

    public async Task<IActionResult> Thread(long id)
    {
        var scope = IsAdmin ? null : await _chat.TeacherIdOfUserAsync(CurrentUserId);
        if (!IsAdmin && scope == null) return Forbid();
        var vm = await _chat.OpenForStaffAsync(id, scope, CurrentUserId);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Send(long id, string? body)
    {
        var scope = IsAdmin ? null : await _chat.TeacherIdOfUserAsync(CurrentUserId);
        if (!IsAdmin && scope == null) return NoPermission();
        var name = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
        var r = await _chat.SendFromStaffAsync(id, scope, CurrentUserId, name, body);
        if (r.Ok && r.Live != null)
            foreach (var userId in r.ToUserIds)
                await _hub.PushAsync(userId, r.Live);
        return Json(new { ok = r.Ok, message = r.Message, id = r.Id, extra = r.When });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        var scope = IsAdmin ? null : await _chat.TeacherIdOfUserAsync(CurrentUserId);
        return Json(await _chat.DeleteConversationAsync(id, scope, IsAdmin, CurrentUserId));
    }
}
