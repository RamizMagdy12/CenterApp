using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _u;
    public DashboardService(IUnitOfWork u) => _u = u;

    private static bool IsPresent(AttendanceStatus s) => s == AttendanceStatus.Present || s == AttendanceStatus.Late;

    public async Task<DashboardVm> GetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var vm = new DashboardVm();

        // ── أرقام عامة
        vm.TotalStudents = await _u.Student.GetAll(s => s.IsActive).CountAsync();
        vm.EnrolledStudents = await _u.GroupEnrollment.GetAll(e => e.IsActive).Select(e => e.StudentId).Distinct().CountAsync();
        vm.ActiveGroups = await _u.Group.GetAll(g => g.IsActive).CountAsync();
        vm.Teachers = await _u.Teacher.GetAll(t => t.IsActive).CountAsync();

        // ── حصص النهارده
        var sessions = await _u.Session
            .GetAll(s => s.Date >= today && s.Date < tomorrow && s.Status != SessionStatus.Cancelled, "Group,Attendances")
            .OrderBy(s => s.StartTime).ToListAsync();

        vm.TodaySessionsCount = sessions.Count;
        vm.TodaySessions = sessions.Select(s => new TodaySessionRow
        {
            GroupName = s.Group.Name,
            Start = s.StartTime,
            End = s.EndTime,
            Status = s.Status,
            Present = s.Attendances.Count(a => IsPresent(a.Status)),
            Total = s.Attendances.Count
        }).ToList();

        var recorded = sessions.SelectMany(s => s.Attendances).ToList();
        vm.TodayPresentRate = recorded.Count == 0 ? null : Math.Round(100.0 * recorded.Count(a => IsPresent(a.Status)) / recorded.Count, 0);

        // ── نسبة الحضور آخر 7 أيام
        var from = today.AddDays(-6);
        var att = await _u.SessionAttendance
            .GetAll(a => a.Session.Date >= from && a.Session.Date < tomorrow)
            .Select(a => new { a.Session.Date, a.Status }).ToListAsync();

        for (var i = 0; i < 7; i++)
        {
            var d = from.AddDays(i);
            var day = att.Where(a => a.Date.Date == d).ToList();
            double? rate = day.Count == 0 ? null : Math.Round(100.0 * day.Count(x => IsPresent(x.Status)) / day.Count, 0);
            vm.Last7.Add(new DayRate(ArabicDays.Name(d.DayOfWeek), rate));
        }

        // ── الطلاب حسب المرحلة
        var byGrade = await _u.Student.GetAll(s => s.IsActive)
            .GroupBy(s => s.GradeLevelId).Select(g => new { Id = g.Key, C = g.Count() }).ToListAsync();
        var names = await _u.GradeLevel.GetAll().ToDictionaryAsync(g => g.Id, g => g.Name);

        vm.Grades = byGrade.OrderByDescending(x => x.C)
            .Select(x => new GradeSlice(x.Id.HasValue && names.TryGetValue(x.Id.Value, out var n) ? n : "بدون مرحلة", x.C))
            .ToList();

        // ── حسابات الشهر
        int y = today.Year, m = today.Month;
        vm.MonthDue = await _u.MonthlyInvoice.GetAll(i => i.Year == y && i.Month == m)
            .SumAsync(i => (decimal?)(i.Amount - i.Discount)) ?? 0;
        vm.MonthCollected = await _u.Payment.GetAll(p => p.Invoice.Year == y && p.Invoice.Month == m)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // ── آخر التحصيلات
        var pays = await _u.Payment.GetAll(includeProperties: "Invoice.Enrollment.Student")
            .OrderByDescending(p => p.PaidAt).Take(6).ToListAsync();
        vm.RecentPayments = pays.Select(p => new RecentPaymentRow
        {
            StudentName = p.Invoice.Enrollment.Student.Name,
            Amount = p.Amount,
            PaidAt = p.PaidAt
        }).ToList();

        return vm;
    }
}