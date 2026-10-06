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

        await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var um = sp.GetRequiredService<UserManager<AppUser>>();

        if (!await rm.RoleExistsAsync(RoleNames.Admin))
            await rm.CreateAsync(new IdentityRole(RoleNames.Admin));

        var adminName = config["Seed:AdminUserName"] ?? "admin";
        if (await um.FindByNameAsync(adminName) == null)
        {
            var admin = new AppUser { UserName = adminName, FullName = "مدير النظام", IsActive = true, EmailConfirmed = true };
            var r = await um.CreateAsync(admin, config["Seed:AdminPassword"] ?? "Admin@123");
            if (r.Succeeded) await um.AddToRoleAsync(admin, RoleNames.Admin);
        }
    }
}