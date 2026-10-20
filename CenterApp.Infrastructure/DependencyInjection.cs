using CenterApp.Application.Abstractions;
using CenterApp.Application.Localization;
using CenterApp.Domain.Identity;
using CenterApp.Infrastructure.Data;
using CenterApp.Infrastructure.Localization;
using CenterApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CenterApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ILocService, LocService>();

        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default"),
                b => b.MigrationsAssembly("CenterApp.Infrastructure")));

        services.AddIdentity<AppUser, IdentityRole>(o =>
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

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IHomeworkService, HomeworkService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IGraceService, GraceService>();
        services.AddScoped<IStudentAccountService, StudentAccountService>();
        services.AddSingleton<IMaterialStorage, MaterialStorage>();
        services.AddSingleton<IHomeworkStorage, HomeworkStorage>();
        services.AddSingleton<IMediaTokenService, MediaTokenService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
