using CenterApp.Domain.Center;

namespace CenterApp.Application.Abstractions;

public interface IHomeworkStorage
{
    Task<(string? Error, HomeworkFile? File)> SaveAsync(long homeworkId, UploadedFile? file);
    string FullPath(HomeworkFile f);
    void DeleteFile(HomeworkFile f);
    void DeleteHomework(long homeworkId);
}
