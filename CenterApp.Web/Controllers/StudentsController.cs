using CenterApp.Service.Models;
using CenterApp.Service;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class StudentsController : Controller
{
    private readonly IStudentService _s;
    private readonly ICatalogService _c;
    public StudentsController(IStudentService s, ICatalogService c) { _s = s; _c = c; }

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
        return PartialView(await _s.GetAsync(id));
    }

    [HttpPost] public async Task<IActionResult> Save(StudentVm vm) => Json(await _s.SaveAsync(vm));
    [HttpPost] public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id));
}