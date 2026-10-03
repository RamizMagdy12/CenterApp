using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _u;
    public StudentService(IUnitOfWork u) => _u = u;

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
            IsActive = s.IsActive
        };
    }

    public async Task<OpResult> SaveAsync(StudentVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) return OpResult.Fail("اسم الطالب مطلوب");

        Student? s;
        if (vm.Id == 0)
        {
            s = new Student();
            var maxId = await _u.Student.GetAll().IgnoreQueryFilters().Select(x => (long?)x.Id).MaxAsync() ?? 0;
            s.Code = string.IsNullOrWhiteSpace(vm.Code) ? "S" + (maxId + 1).ToString("D5") : vm.Code.Trim();
            _u.Student.Add(s);
        }
        else
        {
            s = _u.Student.GetFirstOrDefault(x => x.Id == vm.Id);
            if (s == null) return OpResult.Fail("الطالب غير موجود");
            if (!string.IsNullOrWhiteSpace(vm.Code)) s.Code = vm.Code.Trim();
        }

        s.Name = vm.Name.Trim();
        s.Phone = vm.Phone;
        s.ParentName = vm.ParentName;
        s.ParentPhone = vm.ParentPhone;
        s.GradeLevelId = vm.GradeLevelId;
        s.BirthDate = vm.BirthDate;
        s.Notes = vm.Notes;
        s.IsActive = vm.IsActive;

        await _u.SaveAsync();
        return OpResult.Success();
    }

    public async Task<OpResult> DeleteAsync(long id)
    {
        if (await _u.GroupEnrollment.GetAll(e => e.StudentId == id && e.IsActive).AnyAsync())
            return OpResult.Fail("الطالب مسجل في مجموعات، شيله منها الأول");
        var s = _u.Student.GetFirstOrDefault(x => x.Id == id);
        if (s != null) _u.Student.Remove(s);
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }
}