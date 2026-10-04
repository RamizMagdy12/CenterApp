using CenterApp.Service.Models;

public interface IDashboardService
{
    Task<DashboardVm> GetAsync();
}