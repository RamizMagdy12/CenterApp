using CenterApp.Service.Models;

namespace CenterApp.Service.IRepository;

public interface IExamService
{
    Task<long?> TeacherIdOfUserAsync(string userId);
    Task<long?> StudentIdOfUserAsync(string userId);

    Task<List<OnlineExamRow>> ListOnlineAsync(long? teacherId);
    Task<OnlineExamEditVm?> GetOnlineAsync(long id, long? teacherId);
    Task<OpResult> SaveOnlineAsync(OnlineExamSaveDto dto, long? teacherId);
    Task<OpResult> PublishOnlineAsync(long id, long? teacherId);
    Task<OpResult> DeleteOnlineAsync(long id, long? teacherId);
    Task<OnlineGradesVm?> OnlineGradesAsync(long? examId, long? teacherId);

    Task<List<OfflineExamRow>> ListOfflineAsync(long? teacherId);
    Task<OfflineExamEditVm?> GetOfflineAsync(long id, long? teacherId);
    Task<List<OfflineGradeRow>?> GroupStudentsAsync(long groupId, long? teacherId);
    Task<OpResult> SaveOfflineAsync(OfflineExamSaveDto dto, long? teacherId);
    Task<OpResult> DeleteOfflineAsync(long id, long? teacherId);

    Task<StudentExamsVm?> ListForStudentAsync(long studentId);
    Task<int> OpenCountAsync(long studentId);
    Task<ExamIntroVm?> IntroAsync(long studentId, long examId);
    Task<OpResult> StartAsync(long studentId, long examId);
    Task<ExamTakeVm?> TakeAsync(long studentId, long examId);
    Task<ExamSyncResult> SyncAsync(long studentId, ExamSubmitDto dto, bool finish);
}
