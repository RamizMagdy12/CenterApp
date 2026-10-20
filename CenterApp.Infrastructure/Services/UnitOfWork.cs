// Service/Unitofwork/UnitOfWork.cs
using CenterApp.Domain.Center;
using CenterApp.Infrastructure.Data;
using CenterApp.Application.Abstractions;
using CenterApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Infrastructure.Services;

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
        Material = new Repository<Material>(db);
        MaterialGroup = new Repository<MaterialGroup>(db);
        MaterialFile = new Repository<MaterialFile>(db);
        Homework = new Repository<Homework>(db);
        HomeworkGroup = new Repository<HomeworkGroup>(db);
        HomeworkFile = new Repository<HomeworkFile>(db);
        AccessGrace = new Repository<AccessGrace>(db);
        StudentTransfer = new Repository<StudentTransfer>(db);
        OnlineExam = new Repository<OnlineExam>(db);
        OnlineExamGroup = new Repository<OnlineExamGroup>(db);
        OnlineExamQuestion = new Repository<OnlineExamQuestion>(db);
        OnlineExamChoice = new Repository<OnlineExamChoice>(db);
        OnlineExamAttempt = new Repository<OnlineExamAttempt>(db);
        OnlineExamAnswer = new Repository<OnlineExamAnswer>(db);
        OfflineExam = new Repository<OfflineExam>(db);
        OfflineExamGrade = new Repository<OfflineExamGrade>(db);
        Conversation = new Repository<Conversation>(db);
        ChatMessage = new Repository<ChatMessage>(db);
        UserNotification = new Repository<UserNotification>(db);
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
    public IRepository<Material> Material { get; }
    public IRepository<MaterialGroup> MaterialGroup { get; }
    public IRepository<MaterialFile> MaterialFile { get; }
    public IRepository<Homework> Homework { get; }
    public IRepository<HomeworkGroup> HomeworkGroup { get; }
    public IRepository<HomeworkFile> HomeworkFile { get; }
    public IRepository<AccessGrace> AccessGrace { get; }
    public IRepository<StudentTransfer> StudentTransfer { get; }
    public IRepository<OnlineExam> OnlineExam { get; }
    public IRepository<OnlineExamGroup> OnlineExamGroup { get; }
    public IRepository<OnlineExamQuestion> OnlineExamQuestion { get; }
    public IRepository<OnlineExamChoice> OnlineExamChoice { get; }
    public IRepository<OnlineExamAttempt> OnlineExamAttempt { get; }
    public IRepository<OnlineExamAnswer> OnlineExamAnswer { get; }
    public IRepository<OfflineExam> OfflineExam { get; }
    public IRepository<OfflineExamGrade> OfflineExamGrade { get; }
    public IRepository<Conversation> Conversation { get; }
    public IRepository<ChatMessage> ChatMessage { get; }
    public IRepository<UserNotification> UserNotification { get; }    

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