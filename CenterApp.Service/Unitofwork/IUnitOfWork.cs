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
    IRepository<Material> Material { get; }
    IRepository<MaterialGroup> MaterialGroup { get; }
    IRepository<MaterialFile> MaterialFile { get; }
    IRepository<Homework> Homework { get; }
    IRepository<HomeworkGroup> HomeworkGroup { get; }
    IRepository<HomeworkFile> HomeworkFile { get; }
    IRepository<AccessGrace> AccessGrace { get; }
    IRepository<StudentTransfer> StudentTransfer { get; }
    IRepository<OnlineExam> OnlineExam { get; }
    IRepository<OnlineExamGroup> OnlineExamGroup { get; }
    IRepository<OnlineExamQuestion> OnlineExamQuestion { get; }
    IRepository<OnlineExamChoice> OnlineExamChoice { get; }
    IRepository<OnlineExamAttempt> OnlineExamAttempt { get; }
    IRepository<OnlineExamAnswer> OnlineExamAnswer { get; }
    IRepository<OfflineExam> OfflineExam { get; }
    IRepository<OfflineExamGrade> OfflineExamGrade { get; }

    Task<int> SaveAsync();
    Task<int> SaveInTransactionAsync();
    void Detach(object entity);
}