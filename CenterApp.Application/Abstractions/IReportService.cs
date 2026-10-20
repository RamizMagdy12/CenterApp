using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IReportService
{
    Task<long?> TeacherIdOfUserAsync(string userId);
    Task<ReportPageVm> BuildAsync(long? teacherId, long groupId, long? studentId, DateTime? from, DateTime? to, string? kind);
}
