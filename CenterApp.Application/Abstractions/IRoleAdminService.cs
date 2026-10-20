using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IRoleAdminService
{
    Task<List<RoleRow>> ListAsync();
    Task<RoleVm> GetAsync(string? id);
    Task<OpResult> SaveAsync(RoleVm vm);
    Task<OpResult> DeleteAsync(string id);
}
