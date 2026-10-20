using CenterApp.Domain.Center;
using CenterApp.Domain.Identity;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CenterApp.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _u;
    private readonly IInvoiceService _invoices;
    private readonly UserManager<AppUser> _um;
    public StudentService(IUnitOfWork u, IInvoiceService invoices, UserManager<AppUser> um)
    {
        _u = u;
        _invoices = invoices;
        _um = um;
    }

    public async Task<PagedResult<Student>> SearchAsync(string? q, long? gradeId, int page, int pageSize = 15)
    {
        if (page < 1) page = 1;
        var query = _u.Student.GetAll(includeProperties: "GradeLevel");

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x => x.Name.Contains(q)
                || (x.Phone != null && x.Phone.Contains(q))
                || (x.ParentPhone != null && x.ParentPhone.Contains(q))
                || (x.Code != null && x.Code.Contains(q)));
        }
        if (gradeId.HasValue && gradeId > 0) query = query.Where(x => x.GradeLevelId == gradeId);

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<Student> { Items = items, Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<StudentVm> GetAsync(long id)
    {
        if (id == 0) return new StudentVm();
        var s = await _u.Student.GetAll(x => x.Id == id).FirstOrDefaultAsync();
        if (s == null) return new StudentVm();

        var groups = await _u.GroupEnrollment.GetAll(e => e.StudentId == id && e.IsActive, "Group")
            .Select(e => new { e.GroupId, e.Group.Name, e.Group.Code }).ToListAsync();

        return new StudentVm
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            Phone = s.Phone,
            ParentName = s.ParentName,
            ParentPhone = s.ParentPhone,
            GradeLevelId = s.GradeLevelId,
            BirthDate = s.BirthDate,
            Notes = s.Notes,
            IsActive = s.IsActive,
            EnrolledGroupIds = groups.Select(g => g.GroupId).ToList(),
            GroupNames = groups.Select(g => string.IsNullOrEmpty(g.Code) ? g.Name : $"{g.Name} ({g.Code})").ToList()
        };
    }

    public async Task<OpResult> SaveAsync(StudentVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) return OpResult.Fail("اسم الطالب مطلوب");

        var isNew = vm.Id == 0;
        Student s;

        if (isNew)
        {
            if (!vm.GroupId.HasValue)
                return OpResult.Fail("لازم تختار المجموعة، الطالب مينفعش يتسجّل من غير مجموعة");

            var group = await _u.Group.GetAll(g => g.Id == vm.GroupId && g.IsActive, tracking: true).FirstOrDefaultAsync();
            if (group == null) return OpResult.Fail("المجموعة غير موجودة أو متوقفة");
            if (string.IsNullOrWhiteSpace(group.Code))
                return OpResult.Fail("المجموعة دي ملهاش كود، افتحها من شاشة المجموعات وحط لها كود الأول");

            var taken = await _u.GroupEnrollment.GetAll(e => e.GroupId == group.Id && e.IsActive).CountAsync();
            if (group.Capacity > 0 && taken >= group.Capacity) return OpResult.Fail("المجموعة مكتملة العدد");

            group.LastStudentNumber++;
            s = new Student
            {
                Code = $"{group.LastStudentNumber:D3}{group.Code}",
                CodeGroupId = group.Id,
                QrToken = await NewTokenAsync()
            };
            s.Enrollments.Add(new GroupEnrollment { GroupId = group.Id, JoinedAt = DateTime.Today, IsActive = true });
            _u.Student.Add(s);
        }
        else
        {
            s = _u.Student.GetFirstOrDefault(x => x.Id == vm.Id)!;
            if (s == null) return OpResult.Fail("الطالب غير موجود");
        }

        s.Name = vm.Name.Trim();
        s.Phone = vm.Phone;
        s.ParentName = vm.ParentName;
        s.ParentPhone = vm.ParentPhone;
        s.GradeLevelId = vm.GradeLevelId;
        s.BirthDate = vm.BirthDate;
        s.Notes = vm.Notes;
        s.IsActive = vm.IsActive;

        try { await _u.SaveAsync(); }
        catch (DbUpdateConcurrencyException) { return OpResult.Fail("فيه طالب اتسجّل في نفس اللحظة، اضغط حفظ تاني"); }
        catch (DbUpdateException) { return OpResult.Fail("حصل تعارض في الكود، اضغط حفظ تاني"); }

        // لو عندك هنا في نسختك نداء لـ EnsureCurrentMonthForStudentAsync أو أي منطق بعد الحفظ، سيبه

        return new OpResult(true, "تم الحفظ", s.Id, $"{s.Code}|{s.QrToken}");
    }

    public async Task<OpResult> DeleteAsync(long id)
    {
        if (await _u.GroupEnrollment.GetAll(e => e.StudentId == id && e.IsActive).AnyAsync())
            return OpResult.Fail("الطالب مسجل في مجموعات، شيله منها الأول");
        var s = _u.Student.GetFirstOrDefault(x => x.Id == id);
        if (s != null)
        {
            if (!string.IsNullOrEmpty(s.UserId))
            {
                var user = await _um.FindByIdAsync(s.UserId);
                if (user != null)
                {
                    var deleted = await _um.DeleteAsync(user);
                    if (!deleted.Succeeded)
                        return OpResult.Fail(string.Join("، ", deleted.Errors.Select(e => e.Description)));
                }
                s.UserId = null;
            }
            _u.Student.Remove(s);
        }
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }

    public async Task<List<StudentCardVm>> GetCardsAsync(long? studentId, long? groupId)
    {
        IQueryable<Student> q;
        if (studentId.HasValue)
            q = _u.Student.GetAll(x => x.Id == studentId.Value);
        else if (groupId.HasValue)
        {
            var ids = _u.GroupEnrollment.GetAll(e => e.GroupId == groupId.Value && e.IsActive).Select(e => e.StudentId);
            q = _u.Student.GetAll(x => ids.Contains(x.Id));
        }
        else return new List<StudentCardVm>();

        return await q.OrderBy(x => x.Name)
                      .Select(x => new StudentCardVm { Id = x.Id, Name = x.Name, Code = x.Code ?? "", QrToken = x.QrToken ?? x.Code ?? "" })
                      .ToListAsync();
    }
    private async Task<string> NewTokenAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var t = new string(Enumerable.Range(0, 12)
                .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());

            if (!await _u.Student.GetAll(x => x.QrToken == t || x.Code == t).IgnoreQueryFilters().AnyAsync())
                return t;
        }
    }
}
