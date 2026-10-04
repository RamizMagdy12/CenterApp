using CenterApp.Service.Models;
using CenterApp.Service;
using Microsoft.AspNetCore.Mvc;
using QRCoder;


namespace CenterApp.Web.Controllers;

public class StudentsController : Controller
{
    private readonly IStudentService _s;
    private readonly ICatalogService _c;
    private readonly IGroupService _g;
    public StudentsController(IStudentService s, ICatalogService c, IGroupService g) { _s = s; _c = c; _g = g; }

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
        ViewBag.Groups = await _g.SelectAsync();
        return PartialView(await _s.GetAsync(id));
    }

    [HttpPost] public async Task<IActionResult> Save(StudentVm vm) => Json(await _s.SaveAsync(vm));
    [HttpPost] public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id));

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
}