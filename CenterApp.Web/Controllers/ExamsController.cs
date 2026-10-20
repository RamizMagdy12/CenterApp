using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
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

    [HttpPost]
    public async Task<IActionResult> Stop(long id)
    {
        if (!IsAdmin) return NoPermission();
        return Json(await _s.StopOnlineAsync(id));
    }

    [HttpPost, RequirePermission(Screens.Exams, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteOnlineAsync(id, await ScopeAsync()));

    public async Task<IActionResult> Grades(long id = 0)
    {
        var vm = await _s.OnlineGradesAsync(id == 0 ? null : id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reopen(long examId, long studentId, int minutes)
    {
        if (!Can(Screens.Exams, PermissionAction.Edit)) return NoPermission();
        return Json(await _s.ReopenAsync(examId, studentId, minutes, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Exams, PermissionAction.Edit)]
    public async Task<IActionResult> QuestionImage(long examId, int sortOrder, IFormFile file)
    {
        using var uploaded = Uploads.Read(file);
        return Json(await _s.SaveQuestionImageAsync(examId, sortOrder, uploaded, await ScopeAsync()));
    }

    [HttpGet, RequirePermission(Screens.Exams, PermissionAction.View)]
    public async Task<IActionResult> QuestionImage(long examId, string name)
    {
        var opened = await _s.OpenQuestionImageAsync(examId, name, await ScopeAsync());
        if (opened == null) return NotFound();
        return PhysicalFile(opened.Value.Path, opened.Value.ContentType);
    }
}
