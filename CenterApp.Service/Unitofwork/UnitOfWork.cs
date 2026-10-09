// Service/Unitofwork/UnitOfWork.cs
using CenterApp.Entity.Center;
using CenterApp.Service.Data;
using CenterApp.Service.IRepository;
using CenterApp.Service.Repository;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Service.Unitofwork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        GradeLevel = new Repository<GradeLevel>(db);
        Subject = new Repository<Subject>(db);
        Teacher = new Repository<Teacher>(db);
        Student = new Repository<Student>(db);
        Group = new Repository<Group>(db);
        GroupSchedule = new Repository<GroupSchedule>(db);
        GroupEnrollment = new Repository<GroupEnrollment>(db);
        Session = new Repository<Session>(db);
        SessionAttendance = new Repository<SessionAttendance>(db);
        MonthlyInvoice = new Repository<MonthlyInvoice>(db);
        Payment = new Repository<Payment>(db);
    }

    public IRepository<GradeLevel> GradeLevel { get; }
    public IRepository<Subject> Subject { get; }
    public IRepository<Teacher> Teacher { get; }
    public IRepository<Student> Student { get; }
    public IRepository<Group> Group { get; }
    public IRepository<GroupSchedule> GroupSchedule { get; }
    public IRepository<GroupEnrollment> GroupEnrollment { get; }
    public IRepository<Session> Session { get; }
    public IRepository<SessionAttendance> SessionAttendance { get; }
    public IRepository<MonthlyInvoice> MonthlyInvoice { get; }
    public IRepository<Payment> Payment { get; }

    public Task<int> SaveAsync() => _db.SaveChangesAsync();

    public async Task<int> SaveInTransactionAsync()
    {
        if (_db.Database.CurrentTransaction != null)
            return await SaveAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        var count = await SaveAsync();
        await tx.CommitAsync();
        return count;
    }

    public void Detach(object entity) => _db.Entry(entity).State = EntityState.Detached;

    public void Dispose() => _db.Dispose();
}