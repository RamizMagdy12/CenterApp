using CenterApp.Entity.Identity;
using CenterApp.Service.Data;
using CenterApp.Service.IRepository;
using CenterApp.Service.Repository;
using CenterApp.Service.Services;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// كل الصفحات محتاجة تسجيل دخول (ماعدا اللي عليها AllowAnonymous)
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AuthorizeFilter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        b => b.MigrationsAssembly("CenterApp.Service")));

builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireDigit = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireLowercase = false;
    o.Password.RequiredLength = 6;

    o.Lockout.AllowedForNewUsers = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<AppClaimsFactory>();

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

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IRoleAdminService, RoleAdminService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IHomeworkService, HomeworkService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IPortalService, PortalService>();
builder.Services.AddScoped<IGraceService, GraceService>();
builder.Services.AddScoped<IStudentAccountService, StudentAccountService>();
builder.Services.AddSingleton<IMaterialStorage, MaterialStorage>();
builder.Services.AddSingleton<IHomeworkStorage, HomeworkStorage>();
builder.Services.AddSingleton<IMediaTokenService, MediaTokenService>();
builder.Services.AddScoped<ITransferService, TransferService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbSeeder.SeedAsync(app.Services, app.Configuration);

app.Run();