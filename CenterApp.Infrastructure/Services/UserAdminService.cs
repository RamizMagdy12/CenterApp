using CenterApp.Domain.Center;
using CenterApp.Application.Abstractions;
using CenterApp.Domain.Identity;
using CenterApp.Domain.Security;
using CenterApp.Infrastructure.Data;
using CenterApp.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Infrastructure.Services;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<AppUser> _um;
    private readonly RoleManager<IdentityRole> _rm;
    private readonly AppDbContext _db;

    public UserAdminService(UserManager<AppUser> um, RoleManager<IdentityRole> rm, AppDbContext db)
    { _um = um; _rm = rm; _db = db; }

    private static string Ar(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" => "اسم المستخدم مستخدم قبل كده",
        "DuplicateEmail" => "الإيميل مستخدم قبل كده",
        "InvalidUserName" => "اسم المستخدم لازم يكون حروف إنجليزي وأرقام (و . - _ @ +) بس",
        "InvalidEmail" => "الإيميل غير صحيح",
        "PasswordTooShort" => "كلمة المرور لازم تكون 6 حروف على الأقل",
        _ => e.Description
    };

    private static string Errors(IdentityResult r) => string.Join("، ", r.Errors.Select(Ar));

    private async Task<int> OtherActiveAdminsAsync(string excludeId)
        => (await _um.GetUsersInRoleAsync(RoleNames.Admin)).Count(u => u.IsActive && u.Id != excludeId);

    public async Task<List<UserRow>> ListAsync(string? q)
    {
        var users = _um.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            users = users.Where(u => u.UserName!.Contains(q) || u.FullName.Contains(q)
                || (u.FullNameEn != null && u.FullNameEn.Contains(q))
                || (u.Email != null && u.Email.Contains(q)) || (u.PhoneNumber != null && u.PhoneNumber.Contains(q)));
        }

        var list = await users.OrderBy(u => u.UserName).ToListAsync();
        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           select new { ur.UserId, r.Name }).ToListAsync();

        return list.Select(u =>
        {
            var role = roles.Where(x => x.UserId == u.Id).Select(x => x.Name ?? "").FirstOrDefault() ?? "";
            return new UserRow
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                FullName = Bilingual.Show(u.FullName, u.FullNameEn),
                Email = u.Email,
                Phone = u.PhoneNumber,
                Role = role,
                IsActive = u.IsActive,
                IsAdmin = role == RoleNames.Admin
            };
        }).ToList();
    }
    public async Task<List<SimpleItem>> TeacherOptionsAsync()
    {
        var rows = await _db.Teachers.OrderBy(t => t.Name).Select(t => new { t.Id, t.Name, t.NameEn }).ToListAsync();
        return rows.Select(t => new SimpleItem(t.Id, Bilingual.Show(t.Name, t.NameEn))).ToList();
    }

    private async Task LinkTeacherAsync(string userId, long? teacherId)
    {
        foreach (var old in await _db.Teachers.Where(t => t.UserId == userId && t.Id != teacherId).ToListAsync()) old.UserId = null;
        if (teacherId.HasValue)
        {
            var t = await _db.Teachers.FirstOrDefaultAsync(x => x.Id == teacherId.Value);
            if (t != null) t.UserId = userId;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<UserVm> GetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id)) return new UserVm();
        var u = await _um.FindByIdAsync(id);
        if (u == null) return new UserVm();
        var role = (await _um.GetRolesAsync(u)).FirstOrDefault();
        return new UserVm
        {
            Id = u.Id,
            TeacherId = await _db.Teachers
             .Where(t => t.UserId == u.Id)
             .Select(t => (long?)t.Id)
             .FirstOrDefaultAsync() ?? 0,
            UserName = u.UserName ?? "",
            FullName = u.FullName,
            FullNameEn = u.FullNameEn,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            RoleName = role,
            IsActive = u.IsActive
        };
    }

    public async Task<List<string>> RoleNamesAsync(bool includeAdmin)
        => await _rm.Roles.AsNoTracking()
            .Where(r => includeAdmin || r.Name != RoleNames.Admin)
            .OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();

    public async Task<OpResult> SaveAsync(UserVm vm, bool actorIsAdmin, string actorId)
    {
        var userName = vm.UserName?.Trim() ?? "";
        if (userName.Length == 0) return OpResult.Fail("اسم المستخدم مطلوب");
        if (string.IsNullOrWhiteSpace(vm.RoleName)) return OpResult.Fail("اختار الدور");
        if (!await _rm.RoleExistsAsync(vm.RoleName)) return OpResult.Fail("الدور غير موجود");
        if (vm.RoleName == RoleNames.Admin && !actorIsAdmin) return OpResult.Fail("الأدمن بس هو اللي يدّي دور الأدمن");

        var email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        if (email != null && await _um.Users.AnyAsync(u => u.Email == email && u.Id != vm.Id))
            return OpResult.Fail("الإيميل مستخدم قبل كده");

        if (vm.TeacherId > 0 &&
            await _db.Teachers.AnyAsync(t =>
         t.Id == vm.TeacherId &&
         t.UserId != null &&
         t.UserId != vm.Id))
        {
            return OpResult.Fail("المدرس ده مربوط بمستخدم تاني");
        }

        // ── مستخدم جديد
        if (string.IsNullOrEmpty(vm.Id))
        {
            if (string.IsNullOrEmpty(vm.Password) || vm.Password.Length < 6)
                return OpResult.Fail("كلمة المرور لازم تكون 6 حروف على الأقل");

            var nu = new AppUser
            {
                UserName = userName,
                FullName = vm.FullName?.Trim() ?? "",
                FullNameEn = Bilingual.Clean(vm.FullNameEn),
                Email = email,
                PhoneNumber = vm.PhoneNumber,
                IsActive = vm.IsActive,
                EmailConfirmed = true
            };
            var cr = await _um.CreateAsync(nu, vm.Password);
            if (!cr.Succeeded) return OpResult.Fail(Errors(cr));

            await _um.AddToRoleAsync(nu, vm.RoleName);

            await LinkTeacherAsync(nu.Id, vm.TeacherId);

            return OpResult.Success("تم إنشاء المستخدم");
        }

        // ── تعديل
        var user = await _um.FindByIdAsync(vm.Id);
        if (user == null) return OpResult.Fail("المستخدم غير موجود");

        var oldRoles = await _um.GetRolesAsync(user);
        var wasAdmin = oldRoles.Contains(RoleNames.Admin);

        if (wasAdmin && !actorIsAdmin) return OpResult.Fail("الأدمن بس هو اللي يعدّل مستخدم أدمن");

        if (user.Id == actorId && (!vm.IsActive || !oldRoles.Contains(vm.RoleName)))
            return OpResult.Fail("مينفعش توقف حسابك أو تغيّر دورك بنفسك");

        if (wasAdmin && (vm.RoleName != RoleNames.Admin || !vm.IsActive) && await OtherActiveAdminsAsync(user.Id) == 0)
            return OpResult.Fail("ده آخر أدمن نشط في النظام");


        user.UserName = userName;
        user.FullName = vm.FullName?.Trim() ?? "";
        user.FullNameEn = Bilingual.Clean(vm.FullNameEn);
        user.Email = email;
        user.PhoneNumber = vm.PhoneNumber;
        user.IsActive = vm.IsActive;

        var ur = await _um.UpdateAsync(user);
        if (!ur.Succeeded) return OpResult.Fail(Errors(ur));

        if (oldRoles.Count != 1 || oldRoles[0] != vm.RoleName)
        {
            if (oldRoles.Count > 0) await _um.RemoveFromRolesAsync(user, oldRoles);
            await _um.AddToRoleAsync(user, vm.RoleName);
        }

        if (!vm.IsActive)
            await _um.UpdateSecurityStampAsync(user);

        await LinkTeacherAsync(user.Id, vm.TeacherId);

        return OpResult.Success();
    }

    public async Task<OpResult> ResetPasswordAsync(string id, string newPassword, bool actorIsAdmin)
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            return OpResult.Fail("كلمة المرور لازم تكون 6 حروف على الأقل");

        var user = await _um.FindByIdAsync(id);
        if (user == null) return OpResult.Fail("المستخدم غير موجود");
        if (await _um.IsInRoleAsync(user, RoleNames.Admin) && !actorIsAdmin)
            return OpResult.Fail("الأدمن بس هو اللي يغيّر كلمة مرور أدمن");

        var token = await _um.GeneratePasswordResetTokenAsync(user);
        var r = await _um.ResetPasswordAsync(user, token, newPassword);
        if (!r.Succeeded) return OpResult.Fail(Errors(r));

        await _um.ResetAccessFailedCountAsync(user);
        await _um.SetLockoutEndDateAsync(user, null);
        return OpResult.Success("تم تغيير كلمة المرور");
    }

    public async Task<OpResult> DeleteAsync(string id, bool actorIsAdmin, string actorId)
    {
        if (id == actorId) return OpResult.Fail("مينفعش تحذف حسابك");

        var user = await _um.FindByIdAsync(id);
        if (user == null) return OpResult.Fail("المستخدم غير موجود");

        var isAdmin = await _um.IsInRoleAsync(user, RoleNames.Admin);
        if (isAdmin && !actorIsAdmin) return OpResult.Fail("الأدمن بس هو اللي يحذف مستخدم أدمن");
        if (isAdmin && await OtherActiveAdminsAsync(user.Id) == 0) return OpResult.Fail("ده آخر أدمن نشط في النظام");

        var r = await _um.DeleteAsync(user);
        return r.Succeeded ? OpResult.Success("تم الحذف") : OpResult.Fail(Errors(r));
    }

}
