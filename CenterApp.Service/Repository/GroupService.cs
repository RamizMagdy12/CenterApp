using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _u;
    public GroupService(IUnitOfWork u) => _u = u;

    public async Task<List<GroupRow>> ListAsync(string? q)
    {
        var groups = await _u.Group
            .GetAll(g => string.IsNullOrEmpty(q) || g.Name.Contains(q!), "Teacher,Subject,GradeLevel,Schedules,Enrollments")
            .OrderBy(g => g.Name).ToListAsync();

        return groups.Select(g => new GroupRow
        {
            Id = g.Id,
            Name = g.Name,
            Teacher = g.Teacher?.Name ?? "",
            Subject = g.Subject?.Name ?? "",
            Grade = g.GradeLevel?.Name ?? "",
            Fee = g.MonthlyFee,
            Capacity = g.Capacity,
            IsActive = g.IsActive,
            Enrolled = g.Enrollments.Count(e => e.IsActive),
            Schedule = string.Join(" | ", g.Schedules.OrderBy(s => ArabicDays.Order(s.DayOfWeek))
                .Select(s => $"{ArabicDays.Name(s.DayOfWeek)} {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm}"))
        }).ToList();
    }

    public async Task<GroupSaveDto> GetAsync(long id)
    {
        if (id == 0) return new GroupSaveDto();
        var g = await _u.Group.GetAll(x => x.Id == id, "Schedules").FirstOrDefaultAsync();
        if (g == null) return new GroupSaveDto();
        return new GroupSaveDto
        {
            Id = g.Id,
            Name = g.Name,
            TeacherId = g.TeacherId,
            SubjectId = g.SubjectId,
            GradeLevelId = g.GradeLevelId,
            MonthlyFee = g.MonthlyFee,
            Capacity = g.Capacity,
            IsActive = g.IsActive,
            Schedules = g.Schedules.OrderBy(s => ArabicDays.Order(s.DayOfWeek)).Select(s => new GroupScheduleDto
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime.ToString(@"hh\:mm"),
                EndTime = s.EndTime.ToString(@"hh\:mm"),
                Room = s.Room
            }).ToList()
        };
    }

    public async Task<OpResult> SaveAsync(GroupSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return OpResult.Fail("اسم المجموعة مطلوب");
        if (dto.TeacherId <= 0 || dto.SubjectId <= 0) return OpResult.Fail("اختار المدرس والمادة");
        if (dto.Schedules == null || dto.Schedules.Count == 0) return OpResult.Fail("أضف ميعاد واحد على الأقل");

        var schedules = new List<GroupSchedule>();
        foreach (var s in dto.Schedules)
        {
            if (!TimeSpan.TryParse(s.StartTime, out var st) || !TimeSpan.TryParse(s.EndTime, out var et) || et <= st)
                return OpResult.Fail("ميعاد غير صحيح (وقت النهاية لازم يكون بعد البداية)");
            schedules.Add(new GroupSchedule { DayOfWeek = s.DayOfWeek, StartTime = st, EndTime = et, Room = s.Room });
        }

        Group? g;
        if (dto.Id == 0)
        {
            g = new Group();
            _u.Group.Add(g);
        }
        else
        {
            g = await _u.Group.GetAll(x => x.Id == dto.Id, "Schedules", tracking: true).FirstOrDefaultAsync();
            if (g == null) return OpResult.Fail("المجموعة غير موجودة");
            foreach (var old in g.Schedules.ToList()) _u.GroupSchedule.Remove(old);
        }

        g.Name = dto.Name.Trim();
        g.TeacherId = dto.TeacherId;
        g.SubjectId = dto.SubjectId;
        g.GradeLevelId = dto.GradeLevelId;
        g.MonthlyFee = dto.MonthlyFee;
        g.Capacity = dto.Capacity;
        g.IsActive = dto.IsActive;
        foreach (var s in schedules) g.Schedules.Add(s);

        await _u.SaveAsync();
        return OpResult.Success();
    }

    public async Task<OpResult> DeleteAsync(long id)
    {
        if (await _u.GroupEnrollment.GetAll(e => e.GroupId == id && e.IsActive).AnyAsync())
            return OpResult.Fail("فيه طلاب مسجلين في المجموعة");
        var g = _u.Group.GetFirstOrDefault(x => x.Id == id);
        if (g != null) _u.Group.Remove(g);
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }

    public async Task<List<SelectListItem>> SelectAsync()
        => await _u.Group.GetAll(g => g.IsActive).OrderBy(g => g.Name)
            .Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToListAsync();
}