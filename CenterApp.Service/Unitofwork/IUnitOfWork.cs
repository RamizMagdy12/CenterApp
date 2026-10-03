// Service/Unitofwork/IUnitOfWork.cs
using CenterApp.Entity.Center;
using CenterApp.Service.IRepository;

namespace CenterApp.Service.Unitofwork;

public interface IUnitOfWork : IDisposable
{
    IRepository<GradeLevel> GradeLevel { get; }
    IRepository<Subject> Subject { get; }
    IRepository<Teacher> Teacher { get; }
    IRepository<Student> Student { get; }
    IRepository<Group> Group { get; }
    IRepository<GroupSchedule> GroupSchedule { get; }
    IRepository<GroupEnrollment> GroupEnrollment { get; }
    IRepository<Session> Session { get; }
    IRepository<SessionAttendance> SessionAttendance { get; }
    IRepository<MonthlyInvoice> MonthlyInvoice { get; }
    IRepository<Payment> Payment { get; }

    Task<int> SaveAsync();
}