// Security/AppController.cs
using System.Security.Claims;
using CenterApp.Domain.Security;
using CenterApp.Application.Localization;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Security;

public abstract class AppController : Controller
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    protected bool IsAdmin => User.IsInRole(RoleNames.Admin);

    protected bool Can(string screen, PermissionAction action)
        => HttpContext.RequestServices.GetRequiredService<IPermissionService>().Has(User, screen, action);

    protected IActionResult NoPermission()
        => Json(OpResult.Fail(HttpContext.RequestServices.GetRequiredService<ILocService>().Get("Auth.NoPermission")));
}