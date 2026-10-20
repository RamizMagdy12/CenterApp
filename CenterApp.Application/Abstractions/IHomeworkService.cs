using CenterApp.Domain.Center;
using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IHomeworkService
{
    Task<long?> TeacherIdOfUserAsync(string userId);
    Task<List<HomeworkRow>> ListAsync(long? teacherId);
    Task<HomeworkEditVm?> GetAsync(long id, long? teacherId);
    Task<OpResult> SaveAsync(HomeworkSaveDto dto, long? teacherId);
    Task<OpResult> UploadAsync(long homeworkId, UploadedFile? file, long? teacherId);
    Task<OpResult> DeleteFileAsync(long fileId, long? teacherId);
    Task<OpResult> PublishAsync(long id, long? teacherId);
    Task<OpResult> DeleteAsync(long id, long? teacherId);
    Task<bool> CanViewAsync(long homeworkId, long? teacherId);
    Task<HomeworkFile?> FileAsync(long fileId, long? teacherId);
}
