
using CenterApp.Service.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public interface IGroupService
{
    Task<List<GroupRow>> ListAsync(string? q);
    Task<GroupSaveDto> GetAsync(long id);
    Task<OpResult> SaveAsync(GroupSaveDto dto);
    Task<OpResult> DeleteAsync(long id);
    Task<List<SelectListItem>> SelectAsync();
}