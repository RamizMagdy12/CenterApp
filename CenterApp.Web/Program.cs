using System.Globalization;
using CenterApp.Application.Abstractions;
using CenterApp.Infrastructure;
using CenterApp.Infrastructure.Services;
using CenterApp.Web.Hubs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// كل الصفحات محتاجة تسجيل دخول (ماعدا اللي عليها AllowAnonymous)
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AuthorizeFilter()));
builder.Services.AddLocalization();
builder.Services.AddInfrastructure(builder.Configuration);

var arabic = new CultureInfo("ar-EG");
var english = new CultureInfo("en");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new RequestCulture(arabic, arabic);
    o.SupportedCultures = new[] { arabic, english };
    o.SupportedUICultures = new[] { arabic, english };
    o.RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() };
});

static bool IsAjax(HttpRequest r) => r.Headers["X-Requested-With"] == "XMLHttpRequest";

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(12);
    o.SlidingExpiration = true;

    o.Events.OnValidatePrincipal = async ctx =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(ctx);
        if (ctx.Principal?.Identity?.IsAuthenticated != true)
            ctx.HttpContext.Items["SessionReplaced"] = true;
    };

    // طلبات الـ Ajax ترجع 401 / 403 بدل ما تعمل Redirect لصفحة HTML
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (IsAjax(ctx.Request))
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        var target = ctx.RedirectUri;
        if (ctx.HttpContext.Items.ContainsKey("SessionReplaced"))
            target += (target.Contains('?') ? "&" : "?") + "replaced=1";
        ctx.Response.Redirect(target);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (IsAjax(ctx.Request)) ctx.Response.StatusCode = 403;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// تغيير دور المستخدم أو توقيفه يسري فوراً (الـ Cookie بيتراجع في كل طلب)
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);

builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddSignalR();

// رفع الفيديوهات (حد أقصى 250 ميجا للطلب)
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 262144000);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 262144000);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRequestLocalization();
app.Use(async (ctx, next) =>
{
    var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
    culture.NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
    CultureInfo.CurrentCulture = culture;
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CenterApp.Web.Hubs.AppHub>("/hubs/app");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbSeeder.SeedAsync(app.Services, app.Configuration);

app.Run();