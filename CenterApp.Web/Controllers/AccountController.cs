using CenterApp.Domain.Identity;
using CenterApp.Domain.Security;
using CenterApp.Application.Localization;
using CenterApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _sm;
    private readonly UserManager<AppUser> _um;
    private readonly ILocService _loc;

    public AccountController(SignInManager<AppUser> sm, UserManager<AppUser> um, ILocService loc)
    {
        _sm = sm;
        _um = um;
        _loc = loc;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null, bool replaced = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return User.IsInRole(RoleNames.Student)
                ? RedirectToAction("Index", "Portal")
                : RedirectToAction("Index", "Home");
        }

        return View(new LoginVm
        {
            ReturnUrl = returnUrl,
            Error = replaced ? _loc.Get("Auth.SessionReplaced").Value : null
        });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Error = _loc.Get("Auth.EnterCredentials");
            return View(vm);
        }

        var id = vm.UserName.Trim();
        var user = await _um.FindByNameAsync(id) ?? await _um.FindByEmailAsync(id);

        if (user == null)
        {
            vm.Error = _loc.Get("Auth.BadCredentials");
            return View(vm);
        }
        if (!user.IsActive)
        {
            vm.Error = _loc.Get("Auth.Inactive");
            return View(vm);
        }

        var result = await _sm.CheckPasswordSignInAsync(user, vm.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // الأدمن يفضل فاتح من أكتر من جهاز. باقي الحسابات الجلسة الجديدة بتقفل القديمة.
            if (!await _um.IsInRoleAsync(user, RoleNames.Admin))
                await _um.UpdateSecurityStampAsync(user);
            await _sm.SignInAsync(user, isPersistent: true);

            if (await _um.IsInRoleAsync(user, RoleNames.Student))
                return RedirectToAction("Index", "Portal");

            return !string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)
                ? Redirect(vm.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }

        vm.Error = result.IsLockedOut
            ? _loc.Get("Auth.LockedOut")
            : _loc.Get("Auth.BadCredentials");
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _sm.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string? returnUrl)
    {
        var name = culture == "en" ? "en" : "ar-EG";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(name)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Login));
    }
}