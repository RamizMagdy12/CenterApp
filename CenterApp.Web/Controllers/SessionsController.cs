using CenterApp.Entity.Security;
using CenterApp.Service;
using CenterApp.Service.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Sessions, PermissionAction.View)]

public class SessionsController : AppController
{
    private readonly ISessionService _s;
    private readonly IGroupService _g;
    public SessionsController(ISessionService s, IGroupService g) { _s = s; _g = g; }

    static (int y, int m) ParseMonth(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month) &&
            DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return (d.Year, d.Month);
        return (DateTime.Today.Year, DateTime.Today.Month);
    }

    public async Task<IActionResult> Index(long groupId = 0, string? month = null)
    {
        var (y, m) = ParseMonth(month);
        ViewBag.GroupId = groupId;
        ViewBag.Month = $"{y:D4}-{m:D2}";
        ViewBag.Groups = await _g.SelectAsync();
        return View(await _s.ListAsync(groupId, y, m));
    }

    [HttpPost, RequirePermission(Screens.Sessions, PermissionAction.Add)]
    public async Task<IActionResult> Generate(long groupId, string month)
    {
        var (y, m) = ParseMonth(month);
        return Json(await _s.GenerateAsync(groupId, y, m));
    }

    [HttpPost, RequirePermission(Screens.Sessions, PermissionAction.Edit)]
    public async Task<IActionResult> Cancel(long id) => Json(await _s.CancelAsync(id));
    [RequirePermission(Screens.Sessions, PermissionAction.Edit)]

    public async Task<IActionResult> Attendance(long id)
    {
        var vm = await _s.GetAttendanceAsync(id);
        if (vm == null) return NotFound();
        if (vm.LockReason != null) return Conflict(vm.LockReason);   // بيظهر كـ SweetAlert
        return PartialView(vm);
    }
    [HttpPost, RequirePermission(Screens.Sessions, PermissionAction.Edit)]
    public async Task<IActionResult> Restore(long id) => Json(await _s.RestoreAsync(id));

    [HttpPost, RequirePermission(Screens.Sessions, PermissionAction.Edit)]
    public async Task<IActionResult> SaveAttendance([FromBody] AttendanceSaveDto dto)
        => Json(await _s.SaveAttendanceAsync(dto));

}