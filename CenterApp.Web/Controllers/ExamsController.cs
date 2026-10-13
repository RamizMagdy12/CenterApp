using CenterApp.Entity.Security;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Exams, PermissionAction.View)]
public class ExamsController : AppController
{
    private readonly IExamService _s;
    public ExamsController(IExamService s) => _s = s;

    private async Task<long?> ScopeAsync() => IsAdmin ? null : await _s.TeacherIdOfUserAsync(CurrentUserId);

    public async Task<IActionResult> Index() => View(await _s.ListOnlineAsync(await ScopeAsync()));

    public async Task<IActionResult> Edit(long id = 0)
    {
        if (!Can(Screens.Exams, id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return Forbid();
        var vm = await _s.GetOnlineAsync(id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] OnlineExamSaveDto dto)
    {
        if (!Can(Screens.Exams, dto.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveOnlineAsync(dto, await ScopeAsync()));
    }

    [HttpPost]
    public async Task<IActionResult> Publish(long id)
    {
        if (!Can(Screens.Exams, PermissionAction.Add) && !Can(Screens.Exams, PermissionAction.Edit)) return NoPermission();
        return Json(await _s.PublishOnlineAsync(id, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Exams, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteOnlineAsync(id, await ScopeAsync()));

    public async Task<IActionResult> Grades(long id = 0)
    {
        var vm = await _s.OnlineGradesAsync(id == 0 ? null : id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }
}
