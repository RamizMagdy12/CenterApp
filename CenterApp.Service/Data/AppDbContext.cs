// Service/Data/AppDbContext.cs
using CenterApp.Entity.Base;
using CenterApp.Entity.Center;
using CenterApp.Entity.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using CenterApp.Entity.Security;
using Microsoft.AspNetCore.Identity;

namespace CenterApp.Service.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    private readonly IHttpContextAccessor _http;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor http)
        : base(options) => _http = http;

    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupSchedule> GroupSchedules => Set<GroupSchedule>();
    public DbSet<GroupEnrollment> GroupEnrollments => Set<GroupEnrollment>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionAttendance> SessionAttendances => Set<SessionAttendance>();
    public DbSet<MonthlyInvoice> MonthlyInvoices => Set<MonthlyInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<decimal>().HavePrecision(18, 2);
        b.Properties<string>().HaveMaxLength(250);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Soft-delete filter على كل كيان بيرث BaseEntity
        foreach (var t in mb.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            var p = Expression.Parameter(t.ClrType, "e");
            var body = Expression.Equal(Expression.Property(p, nameof(BaseEntity.IsDeleted)), Expression.Constant(false));
            mb.Entity(t.ClrType).HasQueryFilter(Expression.Lambda(body, p));
        }

        mb.Entity<Student>().HasIndex(x => x.Code).IsUnique();
        mb.Entity<Student>().HasIndex(x => x.Phone);

        mb.Entity<GroupSchedule>().HasIndex(x => new { x.GroupId, x.DayOfWeek });
        mb.Entity<GroupEnrollment>().HasIndex(x => new { x.GroupId, x.StudentId });
        mb.Entity<Session>().HasIndex(x => new { x.GroupId, x.Date });
        mb.Entity<SessionAttendance>().HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
        mb.Entity<MonthlyInvoice>().HasIndex(x => new { x.EnrollmentId, x.Year, x.Month }).IsUnique();

        // منع Cascade غير مقصود (على جداول السنتر بس، مش جداول الـ Identity)
        foreach (var fk in mb.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
                     .SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // الجدول الأسبوعي يتمسح مع المجموعة
        mb.Entity<GroupSchedule>().HasOne(x => x.Group).WithMany(g => g.Schedules)
            .HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

        // صلاحيات الأدوار (بتتمسح مع الدور)
        mb.Entity<RolePermission>(e =>
        {
            e.Property(x => x.ScreenCode).HasMaxLength(50);
            e.HasIndex(x => new { x.RoleId, x.ScreenCode }).IsUnique();
            e.HasOne<IdentityRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var u = _http.HttpContext?.User;
        var audit = new AuditInfo(
            u?.FindFirstValue(ClaimTypes.NameIdentifier),
            u?.Identity?.Name,
            DateTime.UtcNow);

        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.SetCreated(audit);
                    break;
                case EntityState.Modified:
                    e.Entity.SetModified(audit);
                    break;
                case EntityState.Deleted:          // Soft delete
                    e.State = EntityState.Modified;
                    e.Entity.SetDeleted(audit);
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}