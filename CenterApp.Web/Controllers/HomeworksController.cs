using CenterApp.Entity.Security;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Homework, PermissionAction.View)]
public class HomeworksController : AppController
{
    private readonly IHomeworkService _s;
    private readonly IHomeworkStorage _storage;
    public HomeworksController(IHomeworkService s, IHomeworkStorage storage) { _s = s; _storage = storage; }

    private async Task<long?> ScopeAsync() => IsAdmin ? null : await _s.TeacherIdOfUserAsync(CurrentUserId);

    public async Task<IActionResult> Index() => View(await _s.ListAsync(await ScopeAsync()));

    public async Task<IActionResult> Edit(long id = 0)
    {
        if (!Can(Screens.Homework, id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return Forbid();
        var vm = await _s.GetAsync(id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] HomeworkSaveDto dto)
    {
        if (!Can(Screens.Homework, dto.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveAsync(dto, await ScopeAsync()));
    }

    [HttpPost, RequestSizeLimit(31457280), RequestFormLimits(MultipartBodyLengthLimit = 31457280)]
    public async Task<IActionResult> Upload(long homeworkId, IFormFile file)
    {
        if (!Can(Screens.Homework, PermissionAction.Add) && !Can(Screens.Homework, PermissionAction.Edit)) return NoPermission();
        return Json(await _s.UploadAsync(homeworkId, file, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Homework, PermissionAction.Edit)]
    public async Task<IActionResult> DeleteFile(long id) => Json(await _s.DeleteFileAsync(id, await ScopeAsync()));

    [HttpPost]
    public async Task<IActionResult> Publish(long id)
    {
        if (!Can(Screens.Homework, PermissionAction.Add) && !Can(Screens.Homework, PermissionAction.Edit)) return NoPermission();
        return Json(await _s.PublishAsync(id, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Homework, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id, await ScopeAsync()));

    public async Task<IActionResult> Download(long id)
    {
        var f = await _s.FileAsync(id, await ScopeAsync());
        if (f == null) return NotFound();
        var path = _storage.FullPath(f);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, f.ContentType, f.FileName);
    }
}
