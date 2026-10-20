using System.Security.Cryptography;
using CenterApp.Domain.Identity;
using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Identity;



namespace CenterApp.Infrastructure.Services;

public class StudentAccountService : IStudentAccountService
{
    private readonly IUnitOfWork _u;
    private readonly UserManager<AppUser> _um;
    private readonly RoleManager<IdentityRole> _rm;
    public StudentAccountService(IUnitOfWork u, UserManager<AppUser> um, RoleManager<IdentityRole> rm) { _u = u; _um = um; _rm = rm; }

    public async Task<OpResult> EnsureAsync(long studentId)
    {
        var s = _u.Student.GetFirstOrDefault(x => x.Id == studentId);
        if (s == null) return OpResult.Fail("الطالب غير موجود");
        if (string.IsNullOrWhiteSpace(s.Code)) return OpResult.Fail("الطالب ملوش كود");

        var pwd = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var user = string.IsNullOrEmpty(s.UserId) ? null : await _um.FindByIdAsync(s.UserId);

        if (user == null)
        {
            if (!await _rm.RoleExistsAsync(RoleNames.Student)) await _rm.CreateAsync(new IdentityRole(RoleNames.Student));

            user = new AppUser { UserName = s.Code, FullName = s.Name, PhoneNumber = s.Phone, IsActive = true, EmailConfirmed = true };
            var cr = await _um.CreateAsync(user, pwd);
            if (!cr.Succeeded) return OpResult.Fail(string.Join("، ", cr.Errors.Select(e => e.Description)));

            await _um.AddToRoleAsync(user, RoleNames.Student);
            s.UserId = user.Id;
            await _u.SaveAsync();
        }
        else
        {
            var token = await _um.GeneratePasswordResetTokenAsync(user);
            var rr = await _um.ResetPasswordAsync(user, token, pwd);
            if (!rr.Succeeded) return OpResult.Fail(string.Join("، ", rr.Errors.Select(e => e.Description)));
            await _um.SetLockoutEndDateAsync(user, null);
        }

        return new OpResult(true, "تم", null, $"{user.UserName}|{pwd}");
    }
}
