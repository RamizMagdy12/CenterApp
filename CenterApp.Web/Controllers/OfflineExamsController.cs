using CenterApp.Entity.Security;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Exams, PermissionAction.View)]
public class OfflineExamsController : AppController
{
    private readonly IExamService _s;
    public OfflineExamsController(IExamService s) => _s = s;

    private async Task<long?> ScopeAsync() => IsAdmin ? null : await _s.TeacherIdOfUserAsync(CurrentUserId);

    public async Task<IActionResult> Index() => View(await _s.ListOfflineAsync(await ScopeAsync()));

    public async Task<IActionResult> Edit(long id = 0)
    {
        if (!Can(Screens.Exams, id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return Forbid();
        var vm = await _s.GetOfflineAsync(id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }

    public async Task<IActionResult> Students(long groupId)
    {
        var rows = await _s.GroupStudentsAsync(groupId, await ScopeAsync());
        return rows == null ? NotFound() : Json(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] OfflineExamSaveDto dto)
    {
        if (!Can(Screens.Exams, dto.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveOfflineAsync(dto, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Exams, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteOfflineAsync(id, await ScopeAsync()));
}
