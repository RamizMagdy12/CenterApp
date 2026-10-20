using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardVm> GetAsync();
}
