using System.Security.Claims;
using CenterApp.Domain.Center;
using CenterApp.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CenterApp.Infrastructure.Services;

public class AppClaimsFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public AppClaimsFactory(UserManager<AppUser> um, RoleManager<IdentityRole> rm, IOptions<IdentityOptions> options)
        : base(um, rm, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        var name = Bilingual.Show(user.FullName, user.FullNameEn);
        id.AddClaim(new Claim("FullName", string.IsNullOrWhiteSpace(name) ? user.UserName ?? "" : name));
        return id;
    }
}