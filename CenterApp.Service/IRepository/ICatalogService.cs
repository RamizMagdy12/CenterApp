using CenterApp.Service.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public interface ICatalogService
{
    Task<List<LookupVm>> ListAsync(string type);
    Task<LookupVm> GetAsync(string type, long id);
    Task<OpResult> SaveAsync(LookupVm vm);
    Task<OpResult> DeleteAsync(string type, long id);
    Task<List<SelectListItem>> SelectAsync(string type);
}