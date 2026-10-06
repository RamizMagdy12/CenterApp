using CenterApp.Entity.Security;
using CenterApp.Service;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Groups, PermissionAction.View)]

public class EnrollmentsController : AppController
{
    private readonly IEnrollmentService _s;
    public EnrollmentsController(IEnrollmentService s) => _s = s;

    public async Task<IActionResult> Manage(long groupId)
    {
        var vm = await _s.GetManageAsync(groupId);
        if (vm == null) return NotFound();
        return PartialView(vm);
    }

    [HttpPost, RequirePermission(Screens.Groups, PermissionAction.Edit)]
    public async Task<IActionResult> Add(long groupId, long studentId, decimal? customFee)
        => Json(await _s.AddAsync(groupId, studentId, customFee));

    [HttpPost, RequirePermission(Screens.Groups, PermissionAction.Edit)]
    public async Task<IActionResult> Remove(long id) => Json(await _s.RemoveAsync(id));
}