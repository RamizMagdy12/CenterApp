using CenterApp.Domain.Center;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Infrastructure.Services;

public class HomeworkService : IHomeworkService
{
    private readonly IUnitOfWork _u;
    private readonly IHomeworkStorage _storage;
    public HomeworkService(IUnitOfWork u, IHomeworkStorage storage) { _u = u; _storage = storage; }

    private static string Size(long b) => b >= 1048576 ? $"{b / 1048576.0:0.#} MB" : $"{Math.Max(1, b / 1024)} KB";

    public async Task<long?> TeacherIdOfUserAsync(string userId)
        => await _u.Teacher.GetAll(t => t.UserId == userId).Select(t => (long?)t.Id).FirstOrDefaultAsync();

    public async Task<List<HomeworkRow>> ListAsync(long? teacherId)
    {
        var list = await _u.Homework
            .GetAll(m => teacherId == null || m.TeacherId == teacherId, "Groups.Group,Files,Teacher")
            .OrderByDescending(m => m.Id).ToListAsync();

        return list.Select(m => new HomeworkRow
        {
            Id = m.Id,
            Title = m.Title,
            Teacher = m.Teacher?.Name ?? "",
            Status = m.Status,
            PublishedAt = m.PublishedAt,
            Groups = string.Join("، ", m.Groups.Select(g => g.Group.Name)),
            FilesCount = m.Files.Count
        }).ToList();
    }

    public async Task<HomeworkEditVm?> GetAsync(long id, long? teacherId)
    {
        Homework? m = null;
        if (id != 0)
        {
            m = await _u.Homework.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files").FirstOrDefaultAsync();
            if (m == null) return null;
        }

        var selected = m?.Groups.Select(g => g.GroupId).ToHashSet() ?? new HashSet<long>();
        var groups = await _u.Group.GetAll(g => g.IsActive && (teacherId == null || g.TeacherId == teacherId))
            .OrderBy(g => g.Name).Select(g => new { g.Id, g.Name }).ToListAsync();

        return new HomeworkEditVm
        {
            Id = m?.Id ?? 0,
            Title = m?.Title ?? "",
            Description = m?.Description,
            Status = m?.Status ?? HomeworkStatus.Draft,
            Groups = groups.Select(g => new GroupOption { Id = g.Id, Name = g.Name, Selected = selected.Contains(g.Id) }).ToList(),
            Files = m?.Files.OrderBy(f => f.Id).Select(f => new HomeworkFileVm
            {
                Id = f.Id,
                Name = f.FileName,
                Kind = f.Kind,
                Size = Size(f.SizeBytes)
            }).ToList() ?? new()
        };
    }

    public async Task<OpResult> SaveAsync(HomeworkSaveDto dto, long? teacherId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return OpResult.Fail("عنوان الواجب مطلوب");
        var ids = (dto.GroupIds ?? new()).Distinct().ToList();
        if (ids.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");

        var allowed = await _u.Group.GetAll(g => ids.Contains(g.Id) && (teacherId == null || g.TeacherId == teacherId))
            .Select(g => g.Id).ToListAsync();
        if (allowed.Count != ids.Count) return OpResult.Fail("فيه مجموعة مش تابعة لك");

        Homework? m;
        if (dto.Id == 0)
        {
            m = new Homework { TeacherId = teacherId };
            _u.Homework.Add(m);
        }
        else
        {
            m = await _u.Homework.GetAll(x => x.Id == dto.Id && (teacherId == null || x.TeacherId == teacherId), "Groups", tracking: true)
                .FirstOrDefaultAsync();
            if (m == null) return OpResult.Fail("الواجب غير موجود");
        }

        m.Title = dto.Title.Trim();
        m.Description = dto.Description?.Trim();

        foreach (var g in m.Groups.Where(g => !ids.Contains(g.GroupId)).ToList()) _u.HomeworkGroup.Remove(g);
        foreach (var gid in ids.Where(i => !m.Groups.Any(g => g.GroupId == i))) m.Groups.Add(new HomeworkGroup { GroupId = gid });

        await _u.SaveAsync();
        return new OpResult(true, "تم الحفظ", m.Id);
    }

    public async Task<OpResult> UploadAsync(long homeworkId, UploadedFile? file, long? teacherId)
    {
        if (!await CanViewAsync(homeworkId, teacherId)) return OpResult.Fail("الواجب غير موجود");

        var (err, f) = await _storage.SaveAsync(homeworkId, file);
        if (err != null || f == null) return OpResult.Fail(err ?? "فشل الرفع");

        _u.HomeworkFile.Add(f);
        await _u.SaveAsync();
        return new OpResult(true, "تم رفع الملف", f.Id, f.FileName);
    }

    public async Task<OpResult> DeleteFileAsync(long fileId, long? teacherId)
    {
        var f = await _u.HomeworkFile
            .GetAll(x => x.Id == fileId && (teacherId == null || x.Homework.TeacherId == teacherId), "Homework", tracking: true)
            .FirstOrDefaultAsync();
        if (f == null) return OpResult.Fail("الملف غير موجود");

        if (f.Homework.Status == HomeworkStatus.Published &&
            !await _u.HomeworkFile.GetAll(x => x.HomeworkId == f.HomeworkId && x.Id != f.Id).AnyAsync())
            return OpResult.Fail("الواجب المنشور لازم يفضل فيه ملف واحد على الأقل");

        _storage.DeleteFile(f);
        _u.HomeworkFile.Remove(f);
        await _u.SaveAsync();
        return OpResult.Success("تم حذف الملف");
    }

    public async Task<OpResult> PublishAsync(long id, long? teacherId)
    {
        var m = await _u.Homework.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files", tracking: true)
            .FirstOrDefaultAsync();
        if (m == null) return OpResult.Fail("الواجب غير موجود");
        if (m.Files.Count == 0) return OpResult.Fail("ارفع ملف واحد على الأقل قبل النشر");
        if (m.Groups.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");

        if (m.Status != HomeworkStatus.Published)
        {
            m.Status = HomeworkStatus.Published;
            m.PublishedAt ??= DateTime.Now;
        }
        await _u.SaveAsync();
        return OpResult.Success("تم النشر");
    }

    public async Task<OpResult> DeleteAsync(long id, long? teacherId)
    {
        var m = await _u.Homework.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files", tracking: true)
            .FirstOrDefaultAsync();
        if (m == null) return OpResult.Fail("الواجب غير موجود");

        foreach (var g in m.Groups.ToList()) _u.HomeworkGroup.Remove(g);
        foreach (var f in m.Files.ToList()) _u.HomeworkFile.Remove(f);
        _u.Homework.Remove(m);
        await _u.SaveAsync();

        _storage.DeleteHomework(id);
        return OpResult.Success("تم الحذف");
    }

    public async Task<bool> CanViewAsync(long homeworkId, long? teacherId)
        => await _u.Homework.GetAll(x => x.Id == homeworkId && (teacherId == null || x.TeacherId == teacherId)).AnyAsync();

    public async Task<HomeworkFile?> FileAsync(long fileId, long? teacherId)
        => await _u.HomeworkFile
            .GetAll(f => f.Id == fileId && (teacherId == null || f.Homework.TeacherId == teacherId), "Homework")
            .FirstOrDefaultAsync();
}
