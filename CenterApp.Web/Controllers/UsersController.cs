using CenterApp.Entity.Security;
using CenterApp.Service.Models;
using CenterApp.Service.Services;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Users, PermissionAction.View)]
public class UsersController : AppController
{
    private readonly IUserAdminService _s;
    public UsersController(IUserAdminService s) => _s = s;

    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;
        return View(await _s.ListAsync(q));
    }

    public async Task<IActionResult> Form(string? id)
    {
        ViewBag.Roles = await _s.RoleNamesAsync(IsAdmin);
        return PartialView(await _s.GetAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Save(UserVm vm)
    {
        var action = string.IsNullOrEmpty(vm.Id) ? PermissionAction.Add : PermissionAction.Edit;
        if (!Can(Screens.Users, action)) return NoPermission();
        return Json(await _s.SaveAsync(vm, IsAdmin, CurrentUserId));
    }

    [RequirePermission(Screens.Users, PermissionAction.Edit)]
    public async Task<IActionResult> ResetForm(string id) => PartialView(await _s.GetAsync(id));

    [HttpPost, RequirePermission(Screens.Users, PermissionAction.Edit)]
    public async Task<IActionResult> ResetPassword(string id, string password)
        => Json(await _s.ResetPasswordAsync(id, password, IsAdmin));

    [HttpPost, RequirePermission(Screens.Users, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(string id)
        => Json(await _s.DeleteAsync(id, IsAdmin, CurrentUserId));
}