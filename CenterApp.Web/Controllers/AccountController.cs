using CenterApp.Entity.Identity;
using CenterApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _sm;
    private readonly UserManager<AppUser> _um;

    public AccountController(SignInManager<AppUser> sm, UserManager<AppUser> um) { _sm = sm; _um = um; }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Error = "ادخل اسم المستخدم وكلمة المرور";
            return View(vm);
        }

        var id = vm.UserName.Trim();
        var user = await _um.FindByNameAsync(id) ?? await _um.FindByEmailAsync(id);

        if (user == null)
        {
            vm.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return View(vm);
        }
        if (!user.IsActive)
        {
            vm.Error = "الحساب ده متوقف، كلّم مدير النظام";
            return View(vm);
        }

        var result = await _sm.PasswordSignInAsync(user, vm.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded)
            return !string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)
                ? Redirect(vm.ReturnUrl)
                : RedirectToAction("Index", "Home");

        vm.Error = result.IsLockedOut
            ? "الحساب اتقفل مؤقتاً بسبب محاولات غلط كتير، جرّب بعد 5 دقايق"
            : "اسم المستخدم أو كلمة المرور غير صحيحة";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _sm.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}