using CenterApp.Entity.Identity;
using CenterApp.Entity.Security;
using CenterApp.Service.Data;
using CenterApp.Service.Models;
using CenterApp.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class RoleAdminService : IRoleAdminService
{
    private readonly RoleManager<IdentityRole> _rm;
    private readonly UserManager<AppUser> _um;
    private readonly AppDbContext _db;
    private readonly IPermissionService _perm;

    public RoleAdminService(RoleManager<IdentityRole> rm, UserManager<AppUser> um, AppDbContext db, IPermissionService perm)
    { _rm = rm; _um = um; _db = db; _perm = perm; }

    public async Task<List<RoleRow>> ListAsync()
    {
        var roles = await _rm.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
        var counts = await _db.UserRoles.GroupBy(x => x.RoleId)
            .Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C);

        return roles.Select(r => new RoleRow
        {
            Id = r.Id,
            Name = r.Name ?? "",
            IsAdmin = r.Name == RoleNames.Admin,
            Users = counts.TryGetValue(r.Id, out var c) ? c : 0
        }).ToList();
    }

    public async Task<RoleVm> GetAsync(string? id)
    {
        var perms = Screens.All.Select(s => new RolePermVm { Code = s.Code, Name = s.Name }).ToList();
        if (string.IsNullOrEmpty(id)) return new RoleVm { Perms = perms };

        var role = await _rm.FindByIdAsync(id);
        if (role == null) return new RoleVm { Perms = perms };

        var saved = await _db.RolePermissions.AsNoTracking().Where(p => p.RoleId == id).ToListAsync();
        foreach (var p in perms)
        {
            var s = saved.FirstOrDefault(x => x.ScreenCode == p.Code);
            if (s == null) continue;
            p.View = s.CanView; p.Add = s.CanAdd; p.Edit = s.CanEdit; p.Delete = s.CanDelete;
        }
        return new RoleVm { Id = role.Id, Name = role.Name ?? "", Perms = perms };
    }

    public async Task<OpResult> SaveAsync(RoleVm vm)
    {
        var name = vm.Name?.Trim() ?? "";
        if (name.Length == 0) return OpResult.Fail("اسم الدور مطلوب");

        IdentityRole role;
        if (string.IsNullOrEmpty(vm.Id))
        {
            if (name.Equals(RoleNames.Admin, StringComparison.OrdinalIgnoreCase)) return OpResult.Fail("الاسم ده محجوز للنظام");
            if (await _rm.RoleExistsAsync(name)) return OpResult.Fail("الدور موجود قبل كده");

            role = new IdentityRole(name);
            var cr = await _rm.CreateAsync(role);
            if (!cr.Succeeded) return OpResult.Fail(string.Join("، ", cr.Errors.Select(e => e.Description)));
        }
        else
        {
            var found = await _rm.FindByIdAsync(vm.Id);
            if (found == null) return OpResult.Fail("الدور غير موجود");
            if (found.Name == RoleNames.Admin) return OpResult.Fail("دور الأدمن ثابت ومينفعش يتعدّل");
            if (!string.Equals(found.Name, name, StringComparison.OrdinalIgnoreCase) && await _rm.RoleExistsAsync(name))
                return OpResult.Fail("الدور موجود قبل كده");
            if (name.Equals(RoleNames.Admin, StringComparison.OrdinalIgnoreCase)) return OpResult.Fail("الاسم ده محجوز للنظام");

            role = found;
            role.Name = name;
            var ur = await _rm.UpdateAsync(role);
            if (!ur.Succeeded) return OpResult.Fail(string.Join("، ", ur.Errors.Select(e => e.Description)));
        }

        var existing = await _db.RolePermissions.Where(p => p.RoleId == role.Id).ToListAsync();

        foreach (var s in Screens.All)
        {
            var p = vm.Perms?.FirstOrDefault(x => x.Code == s.Code);
            var row = existing.FirstOrDefault(x => x.ScreenCode == s.Code);
            var any = p != null && (p.View || p.Add || p.Edit || p.Delete);

            if (!any) { if (row != null) _db.RolePermissions.Remove(row); continue; }

            if (row == null)
            {
                row = new RolePermission { RoleId = role.Id, ScreenCode = s.Code };
                _db.RolePermissions.Add(row);
            }
            row.CanView = true;                 // أي صلاحية تانية معاها عرض
            row.CanAdd = p!.Add;
            row.CanEdit = p.Edit;
            row.CanDelete = p.Delete;
        }

        await _db.SaveChangesAsync();
        _perm.InvalidateAll();
        return OpResult.Success("تم حفظ الدور والصلاحيات");
    }

    public async Task<OpResult> DeleteAsync(string id)
    {
        var role = await _rm.FindByIdAsync(id);
        if (role == null) return OpResult.Fail("الدور غير موجود");
        if (role.Name == RoleNames.Admin) return OpResult.Fail("دور الأدمن ثابت ومينفعش يتحذف");
        if ((await _um.GetUsersInRoleAsync(role.Name!)).Any()) return OpResult.Fail("فيه مستخدمين على الدور ده، غيّر أدوارهم الأول");

        var r = await _rm.DeleteAsync(role);
        if (!r.Succeeded) return OpResult.Fail(string.Join("، ", r.Errors.Select(e => e.Description)));

        _perm.InvalidateAll();
        return OpResult.Success("تم الحذف");
    }
}