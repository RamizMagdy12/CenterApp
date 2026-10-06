using CenterApp.Service.Models;

public interface IUserAdminService
{
    Task<List<UserRow>> ListAsync(string? q);
    Task<UserVm> GetAsync(string? id);
    Task<List<string>> RoleNamesAsync(bool includeAdmin);
    Task<OpResult> SaveAsync(UserVm vm, bool actorIsAdmin, string actorId);
    Task<OpResult> ResetPasswordAsync(string id, string newPassword, bool actorIsAdmin);
    Task<OpResult> DeleteAsync(string id, bool actorIsAdmin, string actorId);
}