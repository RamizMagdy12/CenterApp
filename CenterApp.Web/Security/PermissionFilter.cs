// Security/PermissionFilter.cs
using CenterApp.Entity.Security;
using CenterApp.Service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CenterApp.Web.Security;

public class PermissionFilter : IAuthorizationFilter
{
    private readonly IPermissionService _perm;
    private readonly string _screen;
    private readonly PermissionAction _action;

    public PermissionFilter(IPermissionService perm, string screen, PermissionAction action)
    { _perm = perm; _screen = screen; _action = action; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) { context.Result = new ChallengeResult(); return; }
        if (!_perm.Has(user, _screen, _action)) context.Result = new ForbidResult();
    }
}