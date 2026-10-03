using CenterApp.Service.Models;
using CenterApp.Service;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class LookupsController : Controller
{
    private readonly ICatalogService _s;
    public LookupsController(ICatalogService s) => _s = s;

    static readonly Dictionary<string, string> Titles = new()
    {
        ["subjects"] = "المواد",
        ["teachers"] = "المدرسين",
        ["gradelevels"] = "المراحل الدراسية"
    };

    public async Task<IActionResult> Index(string type = "subjects")
    {
        if (!Titles.ContainsKey(type)) type = "subjects";
        ViewBag.Type = type;
        ViewBag.Title = Titles[type];
        return View(await _s.ListAsync(type));
    }

    public async Task<IActionResult> Form(string type, long id = 0)
        => PartialView(await _s.GetAsync(type, id));

    [HttpPost] public async Task<IActionResult> Save(LookupVm vm) => Json(await _s.SaveAsync(vm));
    [HttpPost] public async Task<IActionResult> Delete(string type, long id) => Json(await _s.DeleteAsync(type, id));
}