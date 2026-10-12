using CenterApp.Entity.Security;
using CenterApp.Service;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;
using QRCoder;


namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Students, PermissionAction.View)]

public class StudentsController : AppController
{
    private readonly IStudentService _s;
    private readonly ICatalogService _c;
    private readonly IGroupService _g;
    private readonly IStudentAccountService _acc;
    private readonly ITransferService _t;
    private readonly ISessionService _sessions;

    public StudentsController(IStudentService s, ICatalogService c, IGroupService g, IStudentAccountService acc, ITransferService t, ISessionService sessions)
    { _s = s; _c = c; _g = g; _acc = acc; _t = t; _sessions = sessions; }

    public async Task<IActionResult> Index(string? q, long? gradeId, int page = 1)
    {
        ViewBag.Q = q;
        ViewBag.GradeId = gradeId;
        ViewBag.Grades = await _c.SelectAsync("gradelevels");
        return View(await _s.SearchAsync(q, gradeId, page));
    }

    public async Task<IActionResult> Form(long id = 0)
    {
        ViewBag.Grades = await _c.SelectAsync("gradelevels");
        ViewBag.Picks = id == 0 ? await _t.PicksAsync(null) : new List<GroupPick>();
        return PartialView(await _s.GetAsync(id));
    }

    [RequirePermission(Screens.Groups, PermissionAction.Edit)]
    public async Task<IActionResult> TransferForm(long id, long? from)
    {
        var vm = await _t.FormAsync(id, from);
        return vm == null ? NotFound() : PartialView(vm);
    }

    [HttpPost, RequirePermission(Screens.Groups, PermissionAction.Edit)]
    public async Task<IActionResult> Transfer(long studentId, long fromGroupId, long toGroupId)
        => Json(await _t.TransferAsync(studentId, fromGroupId, toGroupId));

    [HttpPost]
    public async Task<IActionResult> Save(StudentVm vm)
    {
        if (!Can(Screens.Students, vm.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveAsync(vm));
    }
    [HttpPost, RequirePermission(Screens.Students, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id));
    // صورة QR
    [HttpGet]
    public IActionResult Qr(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100) return BadRequest();
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);
        return File(png, "image/png");
    }

    // كارت طالب واحد
    public async Task<IActionResult> Card(long id)
        => View("Cards", await _s.GetCardsAsync(id, null));

    // كروت مجموعة كاملة
    public async Task<IActionResult> Cards(long groupId)
        => View("Cards", await _s.GetCardsAsync(null, groupId));
    [HttpPost, RequirePermission(Screens.Students, PermissionAction.Edit)]
    public async Task<IActionResult> Account(long id) => Json(await _acc.EnsureAsync(id));

    public async Task<IActionResult> Attendance(long id)
    {
        var vm = await _sessions.StudentHistoryAsync(id);
        return vm == null ? NotFound() : View(vm);
    }
}