// Service/Data/AppDbContext.cs
using CenterApp.Domain.Base;
using CenterApp.Domain.Center;
using CenterApp.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using CenterApp.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace CenterApp.Infrastructure.Data;

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
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialGroup> MaterialGroups => Set<MaterialGroup>();
    public DbSet<MaterialFile> MaterialFiles => Set<MaterialFile>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<HomeworkGroup> HomeworkGroups => Set<HomeworkGroup>();
    public DbSet<HomeworkFile> HomeworkFiles => Set<HomeworkFile>();
    public DbSet<AccessGrace> AccessGraces => Set<AccessGrace>();
    public DbSet<StudentTransfer> StudentTransfers => Set<StudentTransfer>();
    public DbSet<OnlineExam> OnlineExams => Set<OnlineExam>();
    public DbSet<OnlineExamGroup> OnlineExamGroups => Set<OnlineExamGroup>();
    public DbSet<OnlineExamQuestion> OnlineExamQuestions => Set<OnlineExamQuestion>();
    public DbSet<OnlineExamChoice> OnlineExamChoices => Set<OnlineExamChoice>();
    public DbSet<OnlineExamAttempt> OnlineExamAttempts => Set<OnlineExamAttempt>();
    public DbSet<OnlineExamAnswer> OnlineExamAnswers => Set<OnlineExamAnswer>();
    public DbSet<OfflineExam> OfflineExams => Set<OfflineExam>();
    public DbSet<OfflineExamGrade> OfflineExamGrades => Set<OfflineExamGrade>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

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
        mb.Entity<Student>().HasIndex(x => x.UserId);
        mb.Entity<Teacher>().HasIndex(x => x.UserId);
        mb.Entity<Material>().Property(x => x.Description).HasMaxLength(2000);
        mb.Entity<Material>().HasIndex(x => new { x.Status, x.PublishedAt });
        mb.Entity<MaterialGroup>().HasIndex(x => new { x.MaterialId, x.GroupId });
        mb.Entity<MaterialFile>().Property(x => x.StoredName).HasMaxLength(100);
        mb.Entity<Homework>().Property(x => x.Description).HasMaxLength(2000);
        mb.Entity<Homework>().HasIndex(x => new { x.Status, x.PublishedAt });
        mb.Entity<HomeworkGroup>().HasIndex(x => new { x.HomeworkId, x.GroupId });
        mb.Entity<HomeworkFile>().Property(x => x.StoredName).HasMaxLength(100);
        mb.Entity<AccessGrace>().HasIndex(x => new { x.StudentId, x.ExpiresAt });
        mb.Entity<Group>().Property(x => x.Code).HasMaxLength(3);
        mb.Entity<Group>().HasIndex(x => x.Code).IsUnique();
        mb.Entity<Group>().Property(x => x.LastStudentNumber).IsConcurrencyToken();   // يمنع رقمين متشابهين لو اتسجّل طالبين في نفس اللحظة
        mb.Entity<Student>().HasIndex(x => x.QrToken).IsUnique();
        mb.Entity<StudentTransfer>().HasIndex(x => x.StudentId);

        mb.Entity<OnlineExam>().Property(x => x.Description).HasMaxLength(2000);
        mb.Entity<OnlineExam>().HasIndex(x => new { x.Status, x.OpensAt });
        mb.Entity<OnlineExamGroup>().HasIndex(x => new { x.ExamId, x.GroupId });
        mb.Entity<OnlineExamQuestion>().Property(x => x.Text).HasMaxLength(2000);
        mb.Entity<OnlineExamQuestion>().Property(x => x.ImageStoredName).HasMaxLength(40);
        mb.Entity<OnlineExamQuestion>().HasIndex(x => new { x.ExamId, x.SortOrder });
        mb.Entity<OnlineExamChoice>().Property(x => x.Text).HasMaxLength(1000);
        mb.Entity<OnlineExamAttempt>().Property(x => x.EndReason).HasMaxLength(250);
        mb.Entity<OnlineExamAttempt>().HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
        mb.Entity<OnlineExamAnswer>().HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        mb.Entity<OfflineExam>().HasIndex(x => new { x.GroupId, x.ExamDate });
        mb.Entity<OfflineExamGrade>().HasIndex(x => new { x.OfflineExamId, x.StudentId }).IsUnique();
        mb.Entity<Conversation>().HasIndex(x => new { x.StudentId, x.TeacherId }).IsUnique();
        mb.Entity<ChatMessage>().Property(x => x.Body).HasMaxLength(2000);
        mb.Entity<ChatMessage>().HasIndex(x => new { x.ConversationId, x.SentAt });
        mb.Entity<UserNotification>().Property(x => x.Body).HasMaxLength(500);
        mb.Entity<UserNotification>().HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });

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
                    e.Entity.SetModified(audit);   // بيسجّل اسم اللي حذف
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}