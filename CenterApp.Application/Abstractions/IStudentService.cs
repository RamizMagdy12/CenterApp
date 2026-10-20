
using CenterApp.Domain.Center;
using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IStudentService
{
    Task<PagedResult<Student>> SearchAsync(string? q, long? gradeId, int page, int pageSize = 15);
    Task<StudentVm> GetAsync(long id);
    Task<OpResult> SaveAsync(StudentVm vm);
    Task<OpResult> DeleteAsync(long id);
    Task<List<StudentCardVm>> GetCardsAsync(long? studentId, long? groupId);
}
