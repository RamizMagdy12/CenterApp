using CenterApp.Entity.Security;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CenterApp.Web.Controllers;

[Authorize(Roles = RoleNames.Student)]
public class PortalController : Controller
{
    private readonly IPortalService _p;
    private readonly IMediaTokenService _tokens;
    public PortalController(IPortalService p, IMediaTokenService tokens) { _p = p; _tokens = tokens; }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public async Task<IActionResult> Index()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();

        var all = await _p.ListAsync(st.Id);
        return View(new PortalHomeVm { Student = st, Latest = all.Take(5).ToList(), Total = all.Count, Locked = all.Count(x => x.IsLocked) });
    }

    public async Task<IActionResult> Materials()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        return View(await _p.ListAsync(st.Id));
    }

    public async Task<IActionResult> Material(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();

        var (row, mat) = await _p.GetAsync(st.Id, id);
        if (row == null || mat == null) return NotFound();

        var vm = new PortalViewVm { Row = row };
        if (!row.IsLocked)
            vm.Viewer = new ViewerVm
            {
                Title = mat.Title,
                Description = mat.Description,
                Watermark = $"{st.Code}  {DateTime.Now:yyyy-MM-dd}",
                Files = mat.Files.OrderBy(f => f.Id).Select(f => new ViewerFileVm
                {
                    Id = f.Id,
                    Name = f.FileName,
                    Kind = f.Kind,
                    PageCount = f.PageCount,
                    Token = _tokens.Create(Uid, f.Id)
                }).ToList()
            };
        return View(vm);
    }
}