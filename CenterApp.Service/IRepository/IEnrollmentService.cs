using CenterApp.Service.Models;

public interface IEnrollmentService
{
    Task<ManageEnrollmentsVm?> GetManageAsync(long groupId);
    Task<OpResult> AddAsync(long groupId, long studentId, decimal? customFee);
    Task<OpResult> RemoveAsync(long enrollmentId);
}