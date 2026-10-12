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
    private readonly ISessionService _sessions;
    private readonly IHomeworkStorage _files;
    public PortalController(IPortalService p, IMediaTokenService tokens, ISessionService sessions, IHomeworkStorage files)
    { _p = p; _tokens = tokens; _sessions = sessions; _files = files; }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public async Task<IActionResult> Index()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();

        var all = await _p.ListAsync(st.Id);
        var hw = await _p.ListHomeworkAsync(st.Id);
        var att = await _sessions.StudentHistoryAsync(st.Id);
        return View(new PortalHomeVm
        {
            Student = st,
            Latest = all.Take(5).ToList(),
            Total = all.Count,
            Locked = all.Count(x => x.IsLocked),
            LatestHomework = hw.Take(5).ToList(),
            HomeworkTotal = hw.Count,
            HomeworkLocked = hw.Count(x => x.IsLocked),
            AttendanceTotal = att?.Total ?? 0,
            AttendancePresent = att?.Attended ?? 0
        });
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

    public async Task<IActionResult> Attendance()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _sessions.StudentHistoryAsync(st.Id);
        return vm == null ? Forbid() : View(vm);
    }

    public async Task<IActionResult> Homework()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        return View(await _p.ListHomeworkAsync(st.Id));
    }

    public async Task<IActionResult> HomeworkItem(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _p.GetHomeworkAsync(st.Id, id);
        return vm == null ? NotFound() : View(vm);
    }

    public async Task<IActionResult> Download(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();

        var f = await _p.HomeworkFileAsync(id);
        if (f == null || !await _p.CanDownloadHomeworkAsync(st.Id, f.HomeworkId)) return Forbid();

        var path = _files.FullPath(f);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, f.ContentType, f.FileName);
    }
}