using CenterApp.Entity.Security;
using CenterApp.Service.IRepository;
using CenterApp.Service.Services;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Grace, PermissionAction.View)]
public class GraceController : AppController
{
    private readonly IGraceService _s;
    public GraceController(IGraceService s) => _s = s;

    public async Task<IActionResult> Index() => View(await _s.ListAsync());

    [RequirePermission(Screens.Grace, PermissionAction.Add)]
    public async Task<IActionResult> Form(long? studentId, long? groupId) => PartialView(await _s.FormAsync(studentId, groupId));

    [HttpPost, RequirePermission(Screens.Grace, PermissionAction.Add)]
    public async Task<IActionResult> Save(long studentId, long? groupId, int days, string? reason)
        => Json(await _s.GrantAsync(studentId, groupId, days, reason));

    [HttpPost, RequirePermission(Screens.Grace, PermissionAction.Edit)]
    public async Task<IActionResult> Revoke(long id) => Json(await _s.RevokeAsync(id));
}