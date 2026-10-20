using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Materials, PermissionAction.View)]
public class MaterialsController : AppController
{
    private readonly IMaterialService _s;
    private readonly IMediaTokenService _tokens;
    public MaterialsController(IMaterialService s, IMediaTokenService tokens) { _s = s; _tokens = tokens; }

    // المدرس بيشوف ماتريال مجموعاته بس، والأدمن (أو موظف مش مربوط بمدرس) بيشوف الكل
    private async Task<long?> ScopeAsync() => IsAdmin ? null : await _s.TeacherIdOfUserAsync(CurrentUserId);

    public async Task<IActionResult> Index() => View(await _s.ListAsync(await ScopeAsync()));

    public async Task<IActionResult> Edit(long id = 0)
    {
        if (!Can(Screens.Materials, id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return Forbid();
        var vm = await _s.GetAsync(id, await ScopeAsync());
        return vm == null ? NotFound() : View(vm);
    }

    public async Task<IActionResult> Preview(long id)
    {
        var vm = await _s.GetAsync(id, await ScopeAsync());
        if (vm == null || vm.Id == 0) return NotFound();

        return View(new ViewerVm
        {
            Title = vm.Title,
            Description = vm.Description,
            Watermark = $"{User.Identity?.Name}  {DateTime.Now:yyyy-MM-dd}",
            Files = vm.Files.Select(f => new ViewerFileVm
            {
                Id = f.Id,
                Name = f.Name,
                Kind = f.Kind,
                PageCount = f.PageCount,
                Token = _tokens.Create(CurrentUserId, f.Id)
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] MaterialSaveDto dto)
    {
        if (!Can(Screens.Materials, dto.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveAsync(dto, await ScopeAsync()));
    }

    [HttpPost, RequestSizeLimit(262144000), RequestFormLimits(MultipartBodyLengthLimit = 262144000)]
    public async Task<IActionResult> Upload(long materialId, IFormFile file)
    {
        if (!Can(Screens.Materials, PermissionAction.Add) && !Can(Screens.Materials, PermissionAction.Edit)) return NoPermission();
        using var uploaded = Uploads.Read(file);
        return Json(await _s.UploadAsync(materialId, uploaded, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Materials, PermissionAction.Edit)]
    public async Task<IActionResult> DeleteFile(long id) => Json(await _s.DeleteFileAsync(id, await ScopeAsync()));

    [HttpPost]
    public async Task<IActionResult> Publish(long id)
    {
        if (!Can(Screens.Materials, PermissionAction.Add) && !Can(Screens.Materials, PermissionAction.Edit)) return NoPermission();
        return Json(await _s.PublishAsync(id, await ScopeAsync()));
    }

    [HttpPost, RequirePermission(Screens.Materials, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id, await ScopeAsync()));
}