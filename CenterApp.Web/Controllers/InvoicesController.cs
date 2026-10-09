using CenterApp.Entity.Center;
using CenterApp.Entity.Security;
using CenterApp.Service;
using CenterApp.Web.Security;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CenterApp.Web.Controllers;

[RequirePermission(Screens.Invoices, PermissionAction.View)]
public class InvoicesController : AppController
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

    public async Task<IActionResult> Index(string? month = null, long groupId = 0, int status = -1, string? q = null, string? sort = null)
    {
        var (y, m) = ParseMonth(month);
        await _s.EnsureMonthAsync(y, m);
        ViewBag.Month = $"{y:D4}-{m:D2}";
        ViewBag.GroupId = groupId;
        ViewBag.Status = status;
        ViewBag.Search = q ?? "";
        ViewBag.Sort = sort ?? "";
        ViewBag.Groups = await _g.SelectAsync();
        return View(await _s.ListAsync(y, m, groupId, status, q, sort));
    }

    [HttpPost, RequirePermission(Screens.Invoices, PermissionAction.Add)]
    public async Task<IActionResult> Generate(string month, long groupId)
    {
        var (y, m) = ParseMonth(month);
        return Json(await _s.GenerateAsync(y, m, groupId));
    }

    [RequirePermission(Screens.Invoices, PermissionAction.Edit)]
    public async Task<IActionResult> Pay(long id)
    {
        var vm = await _s.GetAsync(id);
        if (vm == null) return NotFound();
        return PartialView(vm);
    }

    [HttpPost, RequirePermission(Screens.Invoices, PermissionAction.Edit)]
    public async Task<IActionResult> Pay(long invoiceId, decimal amount, PaymentMethod method, string? note)
        => Json(await _s.PayAsync(invoiceId, amount, method, note));

    [RequirePermission(Screens.Invoices, PermissionAction.Edit)]
    public async Task<IActionResult> Discount(long studentId)
    {
        var vm = await _s.GetDiscountAsync(studentId);
        if (vm == null) return NotFound();
        return PartialView(vm);
    }

    [HttpPost, RequirePermission(Screens.Invoices, PermissionAction.Edit)]
    public async Task<IActionResult> Discount(long studentId, DiscountKind kind, decimal value)
        => Json(await _s.SaveDiscountAsync(studentId, kind, value));
}
