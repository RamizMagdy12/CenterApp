using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Groups, PermissionAction.View)]

public class GroupsController : AppController
{
    private readonly IGroupService _s;
    private readonly ICatalogService _c;
    public GroupsController(IGroupService s, ICatalogService c) { _s = s; _c = c; }

    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;
        return View(await _s.ListAsync(q));
    }

    public async Task<IActionResult> Form(long id = 0)
    {
        ViewBag.Teachers = new SelectList(await _c.SelectAsync("teachers"), "Value", "Text");
        ViewBag.Subjects = new SelectList(await _c.SelectAsync("subjects"), "Value", "Text");
        ViewBag.Grades = new SelectList(await _c.SelectAsync("gradelevels"), "Value", "Text");
        return PartialView(await _s.GetAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] GroupSaveDto dto)
    {
        if (!Can(Screens.Groups, dto.Id == 0 ? PermissionAction.Add : PermissionAction.Edit)) return NoPermission();
        return Json(await _s.SaveAsync(dto));
    }
    [HttpPost, RequirePermission(Screens.Groups, PermissionAction.Delete)]
    public async Task<IActionResult> Delete(long id) => Json(await _s.DeleteAsync(id));
}