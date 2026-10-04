using System.Diagnostics;
using CenterApp.Service.Repository;
using CenterApp.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _d;
    public HomeController(IDashboardService d) => _d = d;

    public async Task<IActionResult> Index() => View(await _d.GetAsync());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}