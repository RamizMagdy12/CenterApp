
using CenterApp.Entity.Center;
using CenterApp.Service.Models;

public interface IStudentService
{
    Task<PagedResult<Student>> SearchAsync(string? q, long? gradeId, int page, int pageSize = 15);
    Task<StudentVm> GetAsync(long id);
    Task<OpResult> SaveAsync(StudentVm vm);
    Task<OpResult> DeleteAsync(long id);
}