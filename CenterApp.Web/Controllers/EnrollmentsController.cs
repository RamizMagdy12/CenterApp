using CenterApp.Service;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Controllers;

public class EnrollmentsController : Controller
{
    private readonly IEnrollmentService _s;
    public EnrollmentsController(IEnrollmentService s) => _s = s;

    public async Task<IActionResult> Manage(long groupId)
    {
        var vm = await _s.GetManageAsync(groupId);
        if (vm == null) return NotFound();
        return PartialView(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Add(long groupId, long studentId, decimal? customFee)
        => Json(await _s.AddAsync(groupId, studentId, customFee));

    [HttpPost]
    public async Task<IActionResult> Remove(long id) => Json(await _s.RemoveAsync(id));
}