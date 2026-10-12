using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _u;
    private readonly IInvoiceService _invoices;
    public EnrollmentService(IUnitOfWork u, IInvoiceService invoices)
    {
        _u = u;
        _invoices = invoices;
    }

    public async Task<ManageEnrollmentsVm?> GetManageAsync(long groupId)
    {
        var g = await _u.Group.GetAll(x => x.Id == groupId).FirstOrDefaultAsync();
        if (g == null) return null;

        var enrolled = await _u.GroupEnrollment
            .GetAll(e => e.GroupId == groupId && e.IsActive, "Student")
            .OrderBy(e => e.Student.Name).ToListAsync();

        var busy = _u.GroupEnrollment.GetAll(e => e.IsActive).Select(e => e.StudentId);
        var free = await _u.Student.GetAll(s => s.IsActive && !busy.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Code }).ToListAsync();

        return new ManageEnrollmentsVm
        {
            GroupId = g.Id,
            GroupName = g.Name,
            Capacity = g.Capacity,
            MonthlyFee = g.MonthlyFee,
            Enrolled = enrolled.Select(e => new EnrolledRow
            {
                EnrollmentId = e.Id,
                StudentId = e.StudentId,
                StudentName = e.Student.Name,
                Phone = e.Student.Phone,
                JoinedAt = e.JoinedAt,
                CustomFee = e.CustomMonthlyFee
            }).ToList(),
            Available = free.Select(s => new SimpleItem(s.Id, $"{s.Name} [{s.Code ?? s.Id.ToString()}]")).ToList()
        };
    }

    public async Task<OpResult> AddAsync(long groupId, long studentId, decimal? customFee)
    {
        var g = await _u.Group.GetAll(x => x.Id == groupId).FirstOrDefaultAsync();
        if (g == null) return OpResult.Fail("المجموعة غير موجودة");

        var current = await _u.GroupEnrollment
            .GetAll(e => e.StudentId == studentId && e.IsActive, "Group")
            .Select(e => new { e.GroupId, e.Group.Name }).FirstOrDefaultAsync();
        if (current != null)
            return current.GroupId == groupId
                ? OpResult.Fail("الطالب مسجل بالفعل")
                : OpResult.Fail($"الطالب مسجل في مجموعة {current.Name}، ومينفعش يكون في أكتر من مجموعة. انقله منها.");

        var count = await _u.GroupEnrollment.GetAll(e => e.GroupId == groupId && e.IsActive).CountAsync();
        if (g.Capacity > 0 && count >= g.Capacity) return OpResult.Fail("المجموعة مكتملة العدد");

        if (customFee is < 0) return OpResult.Fail("السعر غير صحيح");

        _u.GroupEnrollment.Add(new GroupEnrollment
        {
            GroupId = groupId,
            StudentId = studentId,
            JoinedAt = DateTime.Today,
            CustomMonthlyFee = customFee,
            IsActive = true
        });
        await _u.SaveAsync();
        await _invoices.EnsureCurrentMonthForStudentAsync(studentId);
        return OpResult.Success("تم التسجيل");
    }

    public async Task<OpResult> RemoveAsync(long enrollmentId)
    {
        var e = _u.GroupEnrollment.GetFirstOrDefault(x => x.Id == enrollmentId);
        if (e == null) return OpResult.Fail("غير موجود");
        e.IsActive = false;
        e.LeftAt = DateTime.Today;
        await _u.SaveAsync();
        return OpResult.Success("تم إلغاء التسجيل");
    }
}