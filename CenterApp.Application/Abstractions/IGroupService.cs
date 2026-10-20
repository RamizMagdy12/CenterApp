
using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IGroupService
{
    Task<List<GroupRow>> ListAsync(string? q);
    Task<GroupSaveDto> GetAsync(long id);
    Task<OpResult> SaveAsync(GroupSaveDto dto);
    Task<OpResult> DeleteAsync(long id);
    Task<List<LookupItem>> SelectAsync();
}
