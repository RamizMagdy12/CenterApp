using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class SessionService : ISessionService
{
    // التحضير بيفتح قبل بداية الحصة بالدقايق دي (0 = من معاد الحصة بالظبط)
    private const int EarlyMinutes = 15;

    private readonly IUnitOfWork _u;
    public SessionService(IUnitOfWork u) => _u = u;

    private static DateTime StartsAt(Session s) => s.Date.Date.Add(s.StartTime);

    private static string? LockReason(Session s)
    {
        if (s.Status == SessionStatus.Cancelled) return "الحصة دي ملغية";

        var opensAt = StartsAt(s).AddMinutes(-EarlyMinutes);
        if (DateTime.Now < opensAt)
            return $"معاد الحصة لسه ما جاش. التحضير بيفتح يوم {opensAt:yyyy-MM-dd} الساعة {opensAt:HH:mm}";

        return null;
    }

    public async Task<List<SessionRow>> ListAsync(long groupId, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);

        var list = await _u.Session
            .GetAll(s => s.Date >= from && s.Date < to && (groupId == 0 || s.GroupId == groupId), "Group,Attendances")
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync();

        return list.Select(s => new SessionRow
        {
            Id = s.Id,
            GroupName = s.Group.Name,
            Date = s.Date,
            Start = s.StartTime,
            End = s.EndTime,
            Status = s.Status,
            StartsAt = StartsAt(s),
            CanTakeAttendance = LockReason(s) == null,
            Present = s.Attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
            Absent = s.Attendances.Count(a => a.Status == AttendanceStatus.Absent || a.Status == AttendanceStatus.Excused),
            Total = s.Attendances.Count
        }).ToList();
    }

    public async Task<OpResult> GenerateAsync(long groupId, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);

        var groups = await _u.Group.GetAll(g => g.IsActive && (groupId == 0 || g.Id == groupId), "Schedules").ToListAsync();

        var existing = await _u.Session.GetAll(s => s.Date >= from && s.Date < to)
            .Select(s => new { s.GroupId, s.Date, s.StartTime }).ToListAsync();
        var set = existing.Select(e => (e.GroupId, e.Date.Date, e.StartTime)).ToHashSet();

        var toAdd = new List<Session>();
        foreach (var g in groups)
            foreach (var sch in g.Schedules)
                for (var d = from; d < to; d = d.AddDays(1))
                    if (d.DayOfWeek == sch.DayOfWeek && set.Add((g.Id, d, sch.StartTime)))
                        toAdd.Add(new Session
                        {
                            GroupId = g.Id,
                            Date = d,
                            StartTime = sch.StartTime,
                            EndTime = sch.EndTime,
                            Status = SessionStatus.Scheduled
                        });

        if (toAdd.Count == 0) return OpResult.Success("مفيش حصص جديدة (كلها متولدة قبل كده)");
        _u.Session.AddRange(toAdd);
        await _u.SaveAsync();
        return OpResult.Success($"تم توليد {toAdd.Count} حصة");
    }

    public async Task<OpResult> CancelAsync(long id)
    {
        var s = _u.Session.GetFirstOrDefault(x => x.Id == id);
        if (s == null) return OpResult.Fail("غير موجودة");
        s.Status = SessionStatus.Cancelled;
        await _u.SaveAsync();
        return OpResult.Success("تم إلغاء الحصة");
    }

    public async Task<AttendanceVm?> GetAttendanceAsync(long sessionId)
    {
        var s = await _u.Session.GetAll(x => x.Id == sessionId, "Group").FirstOrDefaultAsync();
        if (s == null) return null;

        var reason = LockReason(s);
        if (reason != null) return new AttendanceVm { SessionId = s.Id, LockReason = reason };

        var enrollments = await _u.GroupEnrollment
            .GetAll(e => e.GroupId == s.GroupId && e.JoinedAt <= s.Date && (e.LeftAt == null || e.LeftAt >= s.Date), "Student")
            .OrderBy(e => e.Student.Name).ToListAsync();

        var recorded = (await _u.SessionAttendance.GetAll(a => a.SessionId == sessionId).ToListAsync())
            .ToDictionary(a => a.StudentId, a => a.Status);

        return new AttendanceVm
        {
            SessionId = s.Id,
            GroupName = s.Group.Name,
            Date = s.Date,
            Rows = enrollments.Select(e => new AttendanceRowVm
            {
                StudentId = e.StudentId,
                StudentCode = e.Student.Code ?? "",
                StudentName = e.Student.Name,
                IsRecorded = recorded.ContainsKey(e.StudentId),
                Status = recorded.TryGetValue(e.StudentId, out var st) ? st : AttendanceStatus.Present
            }).ToList()
        };
    }

    public async Task<OpResult> SaveAttendanceAsync(AttendanceSaveDto dto)
    {
        var s = _u.Session.GetFirstOrDefault(x => x.Id == dto.SessionId);
        if (s == null) return OpResult.Fail("الحصة غير موجودة");

        var reason = LockReason(s);
        if (reason != null) return OpResult.Fail(reason);

        var existing = await _u.SessionAttendance.GetAll(a => a.SessionId == dto.SessionId, tracking: true).ToListAsync();

        foreach (var row in dto.Rows)
        {
            var rec = existing.FirstOrDefault(a => a.StudentId == row.StudentId);
            if (rec == null)
                _u.SessionAttendance.Add(new SessionAttendance { SessionId = dto.SessionId, StudentId = row.StudentId, Status = row.Status });
            else
                rec.Status = row.Status;
        }

        s.Status = SessionStatus.Done;
        await _u.SaveAsync();
        return OpResult.Success("تم حفظ الحضور");
    }
    public async Task<OpResult> RestoreAsync(long id)
    {
        var s = await _u.Session.GetAll(x => x.Id == id, "Attendances", tracking: true).FirstOrDefaultAsync();
        if (s == null) return OpResult.Fail("الحصة غير موجودة");
        if (s.Status != SessionStatus.Cancelled) return OpResult.Fail("الحصة مش ملغية");

        s.Status = s.Attendances.Any() ? SessionStatus.Done : SessionStatus.Scheduled;
        await _u.SaveAsync();
        return OpResult.Success("تم استرجاع الحصة");
    }
}