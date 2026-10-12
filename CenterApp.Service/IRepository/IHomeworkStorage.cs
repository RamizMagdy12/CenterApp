using CenterApp.Entity.Center;
using Microsoft.AspNetCore.Http;

namespace CenterApp.Service.IRepository;

public interface IHomeworkStorage
{
    Task<(string? Error, HomeworkFile? File)> SaveAsync(long homeworkId, IFormFile file);
    string FullPath(HomeworkFile f);
    void DeleteFile(HomeworkFile f);
    void DeleteHomework(long homeworkId);
}
