using CenterApp.Entity.Identity;
using CenterApp.Entity.Security;
using CenterApp.Service.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CenterApp.Service.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await db.Students.IgnoreQueryFilters()
                         .Where(s => s.QrToken == null && s.Code != null)
                         .ExecuteUpdateAsync(x => x.SetProperty(s => s.QrToken, s => s.Code));

        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<AppUser>>();

        foreach (var r in new[] { RoleNames.Admin, RoleNames.Teacher, RoleNames.Student })
            if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

        // المدرس: صلاحيات الماتريال والواجبات كاملة (وبيشوف مجموعاته بس)
        var teacher = await rm.FindByNameAsync(RoleNames.Teacher);
        if (teacher != null)
        {
            var hasAny = await db.RolePermissions.AnyAsync(p => p.RoleId == teacher.Id);
            if (!hasAny)
            {
                db.RolePermissions.Add(new RolePermission
                { RoleId = teacher.Id, ScreenCode = Screens.Materials, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true });
            }
            if (!await db.RolePermissions.AnyAsync(p => p.RoleId == teacher.Id && p.ScreenCode == Screens.Homework))
            {
                db.RolePermissions.Add(new RolePermission
                { RoleId = teacher.Id, ScreenCode = Screens.Homework, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true });
            }
            if (!await db.RolePermissions.AnyAsync(p => p.RoleId == teacher.Id && p.ScreenCode == Screens.Exams))
            {
                db.RolePermissions.Add(new RolePermission
                { RoleId = teacher.Id, ScreenCode = Screens.Exams, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true });
            }
            if (!await db.RolePermissions.AnyAsync(p => p.RoleId == teacher.Id && p.ScreenCode == Screens.Reports))
            {
                db.RolePermissions.Add(new RolePermission
                { RoleId = teacher.Id, ScreenCode = Screens.Reports, CanView = true });
            }

            var teachingRoles = await db.RolePermissions
                .Where(p => p.CanView && (p.ScreenCode == Screens.Materials || p.ScreenCode == Screens.Homework || p.ScreenCode == Screens.Exams || p.RoleId == teacher.Id))
                .Select(p => p.RoleId)
                .Distinct()
                .ToListAsync();
            teachingRoles.Add(teacher.Id);
            foreach (var roleId in teachingRoles.Distinct())
            {
                if (await db.RolePermissions.AnyAsync(p => p.RoleId == roleId && p.ScreenCode == Screens.Messages)) continue;
                db.RolePermissions.Add(new RolePermission
                { RoleId = roleId, ScreenCode = Screens.Messages, CanView = true, CanAdd = true, CanEdit = true });
            }
            await db.SaveChangesAsync();
        }

        var adminName = config["Seed:AdminUserName"] ?? "admin";
        if (await um.FindByNameAsync(adminName) == null)
        {
            var admin = new AppUser { UserName = adminName, FullName = "مدير النظام", IsActive = true, EmailConfirmed = true };
            var r = await um.CreateAsync(admin, config["Seed:AdminPassword"] ?? "Admin@123");
            if (r.Succeeded) await um.AddToRoleAsync(admin, RoleNames.Admin);
        }
    }
}