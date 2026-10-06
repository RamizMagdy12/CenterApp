using CenterApp.Entity.Security;
using CenterApp.Service.Models;
using CenterApp.Service.Services;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Roles, PermissionAction.View)]
public class RolesController : AppController
{
    private readonly IRoleAdminService _s;
    public RolesController(IRoleAdminService s) => _s = s;

    public async Task<IActionResult> Index() => View(await _s.ListAsync());

    public async Task<IActionResult> Form(string? id) => PartialView(await _s.GetAsync(id));

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] RoleVm vm)
    {
        var action = string.IsNullOrEmpty(vm.Id) ? PermissionAction.Add : PermissionAction.Edit;
        if (!Can(Screens.Roles, action)) return NoPermission();
        return Json(await _s.SaveAsync(vm));
    }

    [HttpPost, RequirePermission(Screens.Roles, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(string id) => Json(await _s.DeleteAsync(id));
}