using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
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
    private readonly IExamService _exams;
    private readonly IChatService _chat;
    private readonly IChatNotifier _hub;
    public PortalController(IPortalService p, IMediaTokenService tokens, ISessionService sessions, IHomeworkStorage files, IExamService exams, IChatService chat, IChatNotifier hub)
    { _p = p; _tokens = tokens; _sessions = sessions; _files = files; _exams = exams; _chat = chat; _hub = hub; }

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
            AttendancePresent = att?.Attended ?? 0,
            OpenExams = await _exams.OpenCountAsync(st.Id),
            UnreadChats = await _chat.StudentUnreadAsync(st.Id)
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

    public async Task<IActionResult> Exams()
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _exams.ListForStudentAsync(st.Id);
        return vm == null ? Forbid() : View(vm);
    }

    public async Task<IActionResult> Exam(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _exams.IntroAsync(st.Id, id);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> StartExam(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var result = await _exams.StartAsync(st.Id, id);
        if (!result.Ok)
        {
            TempData["ExamError"] = result.Message;
            return RedirectToAction(nameof(Exam), new { id });
        }
        TempData["RetakePass"] = id.ToString();
        return RedirectToAction(nameof(Take), new { id });
    }

    public async Task<IActionResult> Take(long id)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _exams.TakeAsync(st.Id, id);
        if (vm == null) return RedirectToAction(nameof(Exam), new { id });
        if (vm.SingleEntry && TempData["RetakePass"]?.ToString() != id.ToString())
            return RedirectToAction(nameof(Exam), new { id });
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Sync([FromBody] ExamSubmitDto dto)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        return Json(await _exams.SyncAsync(st.Id, dto, finish: false));
    }

    public async Task<IActionResult> ExamImage(long id)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var opened = await _exams.OpenStudentImageAsync(st.Id, id);
        if (opened == null) return NotFound();
        Response.Headers["Cache-Control"] = "no-store";
        return PhysicalFile(opened.Value.Path, opened.Value.ContentType);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitExam([FromBody] ExamSubmitDto dto)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        return Json(await _exams.SyncAsync(st.Id, dto, finish: true));
    }

    public async Task<IActionResult> Ask(long? teacherId)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var vm = await _chat.AskAsync(st.Id, Uid, teacherId);
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AskSend(long teacherId, string? body)
    {
        var st = await _p.StudentOfUserAsync(Uid);
        if (st == null) return Forbid();
        var name = User.FindFirst("FullName")?.Value ?? st.Name;
        var r = await _chat.SendFromStudentAsync(st.Id, Uid, name, teacherId, body);
        if (r.Ok && r.Live != null)
            foreach (var id in r.ToUserIds)
                await _hub.PushAsync(id, r.Live);
        return Json(new { ok = r.Ok, message = r.Message, id = r.Id, extra = r.When });
    }
}