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

        var enrolled = await _u.GroupEnrollment.GetAll(e => e.StudentId == id && e.IsActive)
            .Select(e => e.GroupId).ToListAsync();

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
            EnrolledGroupIds = enrolled
        };
    }

    public async Task<OpResult> SaveAsync(StudentVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) return OpResult.Fail("اسم الطالب مطلوب");

        var isNew = vm.Id == 0;
        var code = vm.Code?.Trim();
        Student? s;

        if (isNew)
        {
            s = new Student();
            if (string.IsNullOrWhiteSpace(code))
                code = await NewCodeAsync();
        }
        else
        {
            s = _u.Student.GetFirstOrDefault(x => x.Id == vm.Id);
            if (s == null) return OpResult.Fail("الطالب غير موجود");
            code = s.Code;   // الكود ثابت ومبيتغيرش
        }

        if (!string.IsNullOrWhiteSpace(code) &&
            await _u.Student.GetAll(x => x.Code == code && x.Id != vm.Id).AnyAsync())
            return OpResult.Fail("الكود ده مستخدم لطالب تاني");

        s.Code = code;
        s.Name = vm.Name.Trim();
        s.Phone = vm.Phone;
        s.ParentName = vm.ParentName;
        s.ParentPhone = vm.ParentPhone;
        s.GradeLevelId = vm.GradeLevelId;
        s.BirthDate = vm.BirthDate;
        s.Notes = vm.Notes;
        s.IsActive = vm.IsActive;

        if (isNew) _u.Student.Add(s);

        // ── تسجيل الطالب في المجموعات المختارة
        var warnings = new List<string>();
        var ids = (vm.GroupIds ?? new()).Distinct().ToList();
        if (ids.Count > 0)
        {
            var groups = await _u.Group.GetAll(g => ids.Contains(g.Id) && g.IsActive).ToListAsync();

            var counts = (await _u.GroupEnrollment.GetAll(e => ids.Contains(e.GroupId) && e.IsActive)
                .GroupBy(e => e.GroupId).Select(g => new { g.Key, C = g.Count() }).ToListAsync())
                .ToDictionary(x => x.Key, x => x.C);

            var already = isNew
                ? new HashSet<long>()
                : (await _u.GroupEnrollment.GetAll(e => e.StudentId == s.Id && e.IsActive && ids.Contains(e.GroupId))
                    .Select(e => e.GroupId).ToListAsync()).ToHashSet();

            foreach (var g in groups)
            {
                if (already.Contains(g.Id)) continue;
                counts.TryGetValue(g.Id, out var c);
                if (g.Capacity > 0 && c >= g.Capacity)
                {
                    warnings.Add($"المجموعة \"{g.Name}\" مكتملة العدد");
                    continue;
                }
                s.Enrollments.Add(new GroupEnrollment { GroupId = g.Id, JoinedAt = DateTime.Today, IsActive = true });
            }
        }

        await _u.SaveAsync();

        var msg = warnings.Count == 0 ? "تم الحفظ" : "تم الحفظ، لكن: " + string.Join("، ", warnings);
        return new OpResult(true, msg, s.Id, s.Code);
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
            .Select(x => new StudentCardVm { Id = x.Id, Name = x.Name, Code = x.Code ?? "" })
            .ToListAsync();
    }
    private async Task<string> NewCodeAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 8)
                .Select(_ => chars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(chars.Length)])
                .ToArray());

            if (!await _u.Student.GetAll(x => x.Code == code).IgnoreQueryFilters().AnyAsync())
                return code;
        }
    }
}