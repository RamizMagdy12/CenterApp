using CenterApp.Service.Models;

public interface IRoleAdminService
{
    Task<List<RoleRow>> ListAsync();
    Task<RoleVm> GetAsync(string? id);
    Task<OpResult> SaveAsync(RoleVm vm);
    Task<OpResult> DeleteAsync(string id);
}