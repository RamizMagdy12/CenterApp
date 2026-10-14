using CenterApp.Service.Models;

namespace CenterApp.Service.IRepository;

public interface IReportService
{
    Task<long?> TeacherIdOfUserAsync(string userId);
    Task<ReportPageVm> BuildAsync(long? teacherId, long groupId, long? studentId, DateTime? from, DateTime? to, string? kind);
}
