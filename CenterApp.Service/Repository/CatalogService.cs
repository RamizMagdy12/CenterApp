using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class CatalogService : ICatalogService
{
    private readonly IUnitOfWork _u;
    public CatalogService(IUnitOfWork u) => _u = u;

    public async Task<List<LookupVm>> ListAsync(string type) => type switch
    {
        "teachers" => await _u.Teacher.GetAll().OrderBy(x => x.Name)
            .Select(x => new LookupVm { Type = "teachers", Id = x.Id, Name = x.Name, Phone = x.Phone, IsActive = x.IsActive })
            .ToListAsync(),
        "gradelevels" => await _u.GradeLevel.GetAll().OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new LookupVm { Type = "gradelevels", Id = x.Id, Name = x.Name, SortOrder = x.SortOrder })
            .ToListAsync(),
        _ => await _u.Subject.GetAll().OrderBy(x => x.Name)
            .Select(x => new LookupVm { Type = "subjects", Id = x.Id, Name = x.Name })
            .ToListAsync()
    };

    public async Task<LookupVm> GetAsync(string type, long id)
    {
        if (id == 0) return new LookupVm { Type = type };
        return (await ListAsync(type)).FirstOrDefault(x => x.Id == id) ?? new LookupVm { Type = type };
    }

    public async Task<OpResult> SaveAsync(LookupVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) return OpResult.Fail("الاسم مطلوب");
        var name = vm.Name.Trim();

        switch (vm.Type)
        {
            case "teachers":
                {
                    var t = vm.Id == 0 ? new Teacher() : _u.Teacher.GetFirstOrDefault(x => x.Id == vm.Id);
                    if (t == null) return OpResult.Fail("غير موجود");
                    t.Name = name; t.Phone = vm.Phone; t.IsActive = vm.IsActive;
                    if (vm.Id == 0) _u.Teacher.Add(t);
                    break;
                }
            case "gradelevels":
                {
                    var g = vm.Id == 0 ? new GradeLevel() : _u.GradeLevel.GetFirstOrDefault(x => x.Id == vm.Id);
                    if (g == null) return OpResult.Fail("غير موجود");
                    g.Name = name; g.SortOrder = vm.SortOrder;
                    if (vm.Id == 0) _u.GradeLevel.Add(g);
                    break;
                }
            default:
                {
                    var s = vm.Id == 0 ? new Subject() : _u.Subject.GetFirstOrDefault(x => x.Id == vm.Id);
                    if (s == null) return OpResult.Fail("غير موجود");
                    s.Name = name;
                    if (vm.Id == 0) _u.Subject.Add(s);
                    break;
                }
        }
        await _u.SaveAsync();
        return OpResult.Success();
    }

    public async Task<OpResult> DeleteAsync(string type, long id)
    {
        switch (type)
        {
            case "teachers":
                {
                    if (await _u.Group.GetAll(g => g.TeacherId == id).AnyAsync()) return OpResult.Fail("المدرس مرتبط بمجموعات");
                    var t = _u.Teacher.GetFirstOrDefault(x => x.Id == id);
                    if (t != null) _u.Teacher.Remove(t);
                    break;
                }
            case "gradelevels":
                {
                    if (await _u.Group.GetAll(g => g.GradeLevelId == id).AnyAsync() ||
                        await _u.Student.GetAll(s => s.GradeLevelId == id).AnyAsync())
                        return OpResult.Fail("المرحلة مستخدمة في طلاب أو مجموعات");
                    var g = _u.GradeLevel.GetFirstOrDefault(x => x.Id == id);
                    if (g != null) _u.GradeLevel.Remove(g);
                    break;
                }
            default:
                {
                    if (await _u.Group.GetAll(g => g.SubjectId == id).AnyAsync()) return OpResult.Fail("المادة مرتبطة بمجموعات");
                    var s = _u.Subject.GetFirstOrDefault(x => x.Id == id);
                    if (s != null) _u.Subject.Remove(s);
                    break;
                }
        }
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }

    public async Task<List<SelectListItem>> SelectAsync(string type) => type switch
    {
        "teachers" => await _u.Teacher.GetAll(t => t.IsActive).OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync(),
        "gradelevels" => await _u.GradeLevel.GetAll().OrderBy(t => t.SortOrder)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync(),
        _ => await _u.Subject.GetAll().OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync()
    };
}