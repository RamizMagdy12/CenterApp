using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Reports, PermissionAction.View)]
public class ReportsController : AppController
{
    private readonly IReportService _s;
    public ReportsController(IReportService s) => _s = s;

    private async Task<long?> ScopeAsync() => IsAdmin ? null : await _s.TeacherIdOfUserAsync(CurrentUserId);

    public async Task<IActionResult> Index(long groupId = 0, long? studentId = null, DateTime? from = null, DateTime? to = null, string? kind = null)
        => View(await _s.BuildAsync(await ScopeAsync(), groupId, studentId, from, to, kind));

    public async Task<IActionResult> Print(long groupId, long? studentId = null, DateTime? from = null, DateTime? to = null, string? kind = null)
    {
        var vm = await _s.BuildAsync(await ScopeAsync(), groupId, studentId, from, to, kind);
        return vm.GroupId == 0 ? NotFound() : View(vm);
    }
}
