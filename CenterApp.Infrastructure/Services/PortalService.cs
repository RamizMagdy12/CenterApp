using CenterApp.Domain.Center;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CenterApp.Infrastructure.Services;

sealed record Enr(long Id, long GroupId, DateTime JoinedAt, DateTime? LeftAt);
sealed record Inv(long EnrollmentId, int Year, int Month, InvoiceStatus Status);
sealed record Gr(long? GroupId, DateTime ExpiresAt);
sealed record Ctx(List<Enr> E, List<Inv> I, List<Gr> G);
sealed record Verdict(bool Visible, bool Locked, string? Reason, DateTime? GraceUntil, string Groups);

public class PortalService : IPortalService
{
    private readonly IUnitOfWork _u;
    private readonly IMemoryCache _cache;
    public PortalService(IUnitOfWork u, IMemoryCache cache) { _u = u; _cache = cache; }

    public async Task<StudentInfo?> StudentOfUserAsync(string userId)
    {
        var key = $"stu:{userId}";
        if (_cache.TryGetValue(key, out StudentInfo? hit)) return hit;

        var s = await _u.Student.GetAll(x => x.UserId == userId && x.IsActive)
            .Select(x => new { x.Id, x.Code, x.Name }).FirstOrDefaultAsync();
        var info = s == null ? null : new StudentInfo(s.Id, s.Code ?? s.Id.ToString(), s.Name);
        if (info != null) _cache.Set(key, info, TimeSpan.FromMinutes(10));
        return info;
    }

    private async Task<Ctx> LoadAsync(long studentId)
    {
        var enr = await _u.GroupEnrollment.GetAll(e => e.StudentId == studentId)
            .Select(e => new Enr(e.Id, e.GroupId, e.JoinedAt, e.LeftAt)).ToListAsync();
        var ids = enr.Select(e => e.Id).ToList();

        var inv = await _u.MonthlyInvoice.GetAll(i => ids.Contains(i.EnrollmentId))
            .Select(i => new Inv(i.EnrollmentId, i.Year, i.Month, i.Status)).ToListAsync();

        var now = DateTime.Now;
        var gr = await _u.AccessGrace.GetAll(g => g.StudentId == studentId && !g.IsRevoked && g.StartsAt <= now && g.ExpiresAt > now)
            .Select(g => new Gr(g.GroupId, g.ExpiresAt)).ToListAsync();

        return new Ctx(enr, inv, gr);
    }

    private static Verdict Evaluate(Material m, Ctx c)
        => EvaluateAccess(m.Status == MaterialStatus.Published, m.PublishedAt,
            m.Groups.Select(g => (g.GroupId, g.Group?.Name ?? "")), c);

    private static Verdict Evaluate(Homework h, Ctx c)
        => EvaluateAccess(h.Status == HomeworkStatus.Published, h.PublishedAt,
            h.Groups.Select(g => (g.GroupId, g.Group?.Name ?? "")), c);

    private static Verdict EvaluateAccess(bool published, DateTime? publishedAt, IEnumerable<(long GroupId, string Name)> groups, Ctx c)
    {
        var list = groups.ToList();
        if (!published || publishedAt == null) return new(false, false, null, null, "");
        var at = publishedAt.Value;
        var gids = list.Select(g => g.GroupId).ToHashSet();

        // الطالب لازم يكون مشترك في المجموعة وقت النشر
        var cand = c.E.Where(e => gids.Contains(e.GroupId) && e.JoinedAt <= at
                                  && (e.LeftAt == null || e.LeftAt.Value.Date >= at.Date)).ToList();
        if (cand.Count == 0) return new(false, false, null, null, "");

        var names = string.Join("، ", list.Where(g => cand.Any(e => e.GroupId == g.GroupId)).Select(g => g.Name));

        var paid = cand.Any(e => c.I.Any(i => i.EnrollmentId == e.Id && i.Year == at.Year && i.Month == at.Month
                                              && (i.Status == InvoiceStatus.Paid || i.Status == InvoiceStatus.Waived)));
        if (paid) return new(true, false, null, null, names);

        var until = c.G.Where(g => g.GroupId == null || cand.Any(e => e.GroupId == g.GroupId))
            .Select(g => (DateTime?)g.ExpiresAt).Max();
        if (until != null) return new(true, false, null, until, names);

        return new(true, true, $"حساب شهر {at.Month}/{at.Year} لسه ما اتدفعش", null, names);
    }

    private static PortalMaterialRow ToRow(Material m, Verdict v) => new()
    {
        Id = m.Id,
        Title = m.Title,
        Description = m.Description,
        Groups = v.Groups,
        PublishedAt = m.PublishedAt!.Value,
        FilesCount = m.Files.Count,
        Videos = m.Files.Count(f => f.Kind == MaterialFileKind.Video),
        Docs = m.Files.Count(f => f.Kind != MaterialFileKind.Video),
        IsLocked = v.Locked,
        Reason = v.Reason,
        GraceUntil = v.GraceUntil
    };

    public async Task<List<PortalMaterialRow>> ListAsync(long studentId)
    {
        var ctx = await LoadAsync(studentId);
        var gids = ctx.E.Select(e => e.GroupId).Distinct().ToList();
        if (gids.Count == 0) return new();

        var mats = await _u.Material
            .GetAll(m => m.Status == MaterialStatus.Published && m.PublishedAt != null && m.Groups.Any(g => gids.Contains(g.GroupId)),
                    "Groups.Group,Files")
            .OrderByDescending(m => m.PublishedAt).ToListAsync();

        var rows = new List<PortalMaterialRow>();
        foreach (var m in mats)
        {
            var v = Evaluate(m, ctx);
            if (v.Visible) rows.Add(ToRow(m, v));
        }
        return rows;
    }

    public async Task<(PortalMaterialRow? Row, Material? Material)> GetAsync(long studentId, long materialId)
    {
        var m = await _u.Material.GetAll(x => x.Id == materialId, "Groups.Group,Files").FirstOrDefaultAsync();
        if (m == null) return (null, null);

        var v = Evaluate(m, await LoadAsync(studentId));
        return v.Visible ? (ToRow(m, v), m) : (null, null);
    }

    public async Task<bool> CanViewAsync(long studentId, long materialId)
    {
        var key = $"acc:{studentId}:{materialId}";
        if (_cache.TryGetValue(key, out bool ok)) return ok;

        var (row, _) = await GetAsync(studentId, materialId);
        ok = row != null && !row.IsLocked;
        _cache.Set(key, ok, TimeSpan.FromSeconds(30));   // طلبات الفيديو بتتكرر، فبنخزّن القرار 30 ثانية
        return ok;
    }

    public async Task<MaterialFile?> FileAsync(long fileId)
        => await _u.MaterialFile.GetAll(f => f.Id == fileId).FirstOrDefaultAsync();

    private static string Size(long b) => b >= 1048576 ? $"{b / 1048576.0:0.#} MB" : $"{Math.Max(1, b / 1024)} KB";

    private static PortalHomeworkRow ToHomework(Homework h, Verdict v) => new()
    {
        Id = h.Id,
        Title = h.Title,
        Description = h.Description,
        Groups = v.Groups,
        PublishedAt = h.PublishedAt!.Value,
        FilesCount = h.Files.Count,
        IsLocked = v.Locked,
        Reason = v.Reason,
        GraceUntil = v.GraceUntil
    };

    public async Task<List<PortalHomeworkRow>> ListHomeworkAsync(long studentId)
    {
        var ctx = await LoadAsync(studentId);
        var gids = ctx.E.Select(e => e.GroupId).Distinct().ToList();
        if (gids.Count == 0) return new();

        var list = await _u.Homework
            .GetAll(h => h.Status == HomeworkStatus.Published && h.PublishedAt != null && h.Groups.Any(g => gids.Contains(g.GroupId)),
                    "Groups.Group,Files")
            .OrderByDescending(h => h.PublishedAt).ToListAsync();

        var rows = new List<PortalHomeworkRow>();
        foreach (var h in list)
        {
            var v = Evaluate(h, ctx);
            if (v.Visible) rows.Add(ToHomework(h, v));
        }
        return rows;
    }

    public async Task<PortalHomeworkVm?> GetHomeworkAsync(long studentId, long homeworkId)
    {
        var h = await _u.Homework.GetAll(x => x.Id == homeworkId, "Groups.Group,Files").FirstOrDefaultAsync();
        if (h == null) return null;

        var v = Evaluate(h, await LoadAsync(studentId));
        if (!v.Visible) return null;

        return new PortalHomeworkVm
        {
            Row = ToHomework(h, v),
            Files = v.Locked ? new() : h.Files.OrderBy(f => f.Id).Select(f => new PortalHomeworkFileVm
            {
                Id = f.Id,
                Name = f.FileName,
                Kind = f.Kind,
                Size = Size(f.SizeBytes)
            }).ToList()
        };
    }

    public async Task<bool> CanDownloadHomeworkAsync(long studentId, long homeworkId)
    {
        var key = $"hw:{studentId}:{homeworkId}";
        if (_cache.TryGetValue(key, out bool ok)) return ok;

        var vm = await GetHomeworkAsync(studentId, homeworkId);
        ok = vm != null && !vm.Row.IsLocked;
        _cache.Set(key, ok, TimeSpan.FromSeconds(30));
        return ok;
    }

    public async Task<HomeworkFile?> HomeworkFileAsync(long fileId)
        => await _u.HomeworkFile.GetAll(f => f.Id == fileId).FirstOrDefaultAsync();
}
