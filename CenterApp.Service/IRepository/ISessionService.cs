using CenterApp.Service.Models;

public interface ISessionService
{
    Task<List<SessionRow>> ListAsync(long groupId, int year, int month);
    Task<OpResult> GenerateAsync(long groupId, int year, int month);
    Task<OpResult> CancelAsync(long id);
    Task<AttendanceVm?> GetAttendanceAsync(long sessionId);
    Task<OpResult> SaveAttendanceAsync(AttendanceSaveDto dto);
    Task<OpResult> RestoreAsync(long id);
}