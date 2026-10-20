using System.Globalization;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using CenterApp.Domain.Center;
using Microsoft.EntityFrameworkCore;
using CenterApp.Application.Abstractions;
namespace CenterApp.Infrastructure.Services;

public class GraceService : IGraceService
{
    private readonly IUnitOfWork _u;
    public GraceService(IUnitOfWork u) => _u = u;

    public async Task<List<GraceRow>> ListAsync()
    {
        var list = await _u.AccessGrace.GetAll(includeProperties: "Student,Group").OrderByDescending(g => g.Id).Take(300).ToListAsync();
        return list.Select(g => new GraceRow
        {
            Id = g.Id,
            StudentName = Bilingual.Show(g.Student.Name, g.Student.NameEn),
            StudentCode = g.Student.Code ?? "",
            GroupName = g.Group == null ? AllGroups() : Bilingual.Show(g.Group.Name, g.Group.NameEn),
            Days = g.Days,
            ExpiresAt = g.ExpiresAt,
            Reason = Bilingual.Show(g.Reason, g.ReasonEn),
            IsRevoked = g.IsRevoked
        }).ToList();
    }

    public async Task<GraceFormVm> FormAsync(long? studentId, long? groupId)
    {
        var vm = new GraceFormVm { StudentId = studentId, GroupId = groupId };

        if (studentId.HasValue)
        {
            var student = await _u.Student.GetAll(s => s.Id == studentId).Select(s => new { s.Name, s.NameEn }).FirstOrDefaultAsync();
            vm.StudentName = student == null ? null : Bilingual.Show(student.Name, student.NameEn);
        }
        else
            vm.Students = (await _u.Student.GetAll(s => s.IsActive).OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name, s.NameEn, s.Code }).ToListAsync())
                .Select(s => new SimpleItem(s.Id, $"{Bilingual.Show(s.Name, s.NameEn)} [{s.Code}]")).ToList();

        var groups = await _u.Group.GetAll(g => g.IsActive).OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name, g.NameEn }).ToListAsync();
        vm.Groups = groups.Select(g => new SimpleItem(g.Id, Bilingual.Show(g.Name, g.NameEn))).ToList();
        return vm;
    }

    private static string AllGroups()
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "All groups" : "كل المجموعات";

    public async Task<OpResult> GrantAsync(long studentId, long? groupId, int days, string? reason, string? reasonEn = null)
    {
        if (days < 1 || days > 90) return OpResult.Fail("عدد الأيام لازم يكون من 1 لـ 90");
        if (!await _u.Student.GetAll(s => s.Id == studentId).AnyAsync()) return OpResult.Fail("اختار الطالب");
        if (groupId.HasValue && !await _u.Group.GetAll(g => g.Id == groupId).AnyAsync()) return OpResult.Fail("المجموعة غير موجودة");

        var now = DateTime.Now;
        var g = new AccessGrace
        {
            StudentId = studentId,
            GroupId = groupId,
            Days = days,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ReasonEn = Bilingual.Clean(reasonEn),
            StartsAt = now,
            ExpiresAt = now.Date.AddDays(days)     // اليوم الأول = النهارده
        };
        _u.AccessGrace.Add(g);
        await _u.SaveAsync();
        return OpResult.Success($"تم السماح لحد {g.ExpiresAt.AddDays(-1):yyyy-MM-dd}");
    }

    public async Task<OpResult> RevokeAsync(long id)
    {
        var g = _u.AccessGrace.GetFirstOrDefault(x => x.Id == id);
        if (g == null) return OpResult.Fail("غير موجود");
        g.IsRevoked = true;
        await _u.SaveAsync();
        return OpResult.Success("تم إلغاء السماح");
    }
}
