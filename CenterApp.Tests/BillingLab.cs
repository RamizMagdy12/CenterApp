using System.Security.Claims;
using CenterApp.Entity.Center;
using CenterApp.Service.Data;
using CenterApp.Service.Models;
using CenterApp.Service.Repository;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

sealed class StickyHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}

public sealed class BillingLab : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public IUnitOfWork Uow { get; }
    public InvoiceService Invoices { get; }
    public StudentService Students { get; }
    public EnrollmentService Enrollments { get; }

    private BillingLab(SqliteConnection connection, IUnitOfWork uow, InvoiceService invoices, StudentService students, EnrollmentService enrollments)
    {
        _connection = connection;
        Uow = uow;
        Invoices = invoices;
        Students = students;
        Enrollments = enrollments;
    }

    public static async Task<BillingLab> Open()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Name, "Cashier")
        ], "test");
        var http = new StickyHttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options, http);
        await db.Database.EnsureCreatedAsync();

        var uow = new UnitOfWork(db);
        var invoices = new InvoiceService(uow);
        return new BillingLab(connection, uow, invoices, new StudentService(uow, invoices), new EnrollmentService(uow, invoices));
    }

    public async Task<long> AddGroup(string name, decimal monthlyFee)
    {
        var teacher = new Teacher { Name = name + " teacher", IsActive = true };
        var subject = new Subject { Name = name + " subject" };
        Uow.Teacher.Add(teacher);
        Uow.Subject.Add(subject);
        await Uow.SaveAsync();

        var group = new Group
        {
            Name = name,
            TeacherId = teacher.Id,
            SubjectId = subject.Id,
            MonthlyFee = monthlyFee,
            IsActive = true
        };
        Uow.Group.Add(group);
        await Uow.SaveAsync();
        return group.Id;
    }

    public async Task<long> AddStudent(string name, params long[] groupIds)
    {
        var result = await Students.SaveAsync(new StudentVm
        {
            Name = name,
            IsActive = true,
            GroupIds = groupIds.ToList()
        });
        if (!result.Ok || result.Id == null) throw new InvalidOperationException(result.Message);
        return result.Id.Value;
    }

    public async Task<InvoiceRow> CurrentInvoice(long studentId)
    {
        var today = DateTime.Today;
        var rows = await Invoices.ListAsync(today.Year, today.Month, 0, -1);
        return rows.Single(r => r.StudentId == studentId);
    }

    public async Task BackdateEnrollment(long studentId)
    {
        var enrollment = Uow.GroupEnrollment.GetFirstOrDefault(e => e.StudentId == studentId, tracking: true);
        if (enrollment == null) throw new InvalidOperationException("Enrollment was not found.");
        enrollment.JoinedAt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await Uow.SaveAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Uow.Dispose();
        await _connection.DisposeAsync();
    }
}
