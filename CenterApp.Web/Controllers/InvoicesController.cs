using CenterApp.Entity.Center;
using CenterApp.Service;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CenterApp.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _s;
    private readonly IGroupService _g;
    public InvoicesController(IInvoiceService s, IGroupService g) { _s = s; _g = g; }

    static (int y, int m) ParseMonth(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month) &&
            DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return (d.Year, d.Month);
        return (DateTime.Today.Year, DateTime.Today.Month);
    }

    public async Task<IActionResult> Index(string? month = null, long groupId = 0, int status = -1)
    {
        var (y, m) = ParseMonth(month);
        ViewBag.Month = $"{y:D4}-{m:D2}";
        ViewBag.GroupId = groupId;
        ViewBag.Status = status;
        ViewBag.Groups = await _g.SelectAsync();
        return View(await _s.ListAsync(y, m, groupId, status));
    }

    [HttpPost]
    public async Task<IActionResult> Generate(string month, long groupId)
    {
        var (y, m) = ParseMonth(month);
        return Json(await _s.GenerateAsync(y, m, groupId));
    }

    public async Task<IActionResult> Pay(long id)
    {
        var vm = await _s.GetAsync(id);
        if (vm == null) return NotFound();
        return PartialView(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Pay(long invoiceId, decimal amount, decimal discount, PaymentMethod method, string? note)
        => Json(await _s.PayAsync(invoiceId, amount, discount, method, note));
}