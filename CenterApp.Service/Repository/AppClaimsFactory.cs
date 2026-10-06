using System.Security.Claims;
using CenterApp.Entity.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CenterApp.Service.Services;

public class AppClaimsFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public AppClaimsFactory(UserManager<AppUser> um, RoleManager<IdentityRole> rm, IOptions<IdentityOptions> options)
        : base(um, rm, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        id.AddClaim(new Claim("FullName", string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "" : user.FullName));
        return id;
    }
}