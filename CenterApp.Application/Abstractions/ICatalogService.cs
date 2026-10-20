using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface ICatalogService
{
    Task<List<LookupVm>> ListAsync(string type);
    Task<LookupVm> GetAsync(string type, long id);
    Task<OpResult> SaveAsync(LookupVm vm);
    Task<OpResult> DeleteAsync(string type, long id);
    Task<List<LookupItem>> SelectAsync(string type);
}
