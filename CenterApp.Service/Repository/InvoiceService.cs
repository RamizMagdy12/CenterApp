using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _u;
    public InvoiceService(IUnitOfWork u) => _u = u;

    private static InvoiceRow Map(MonthlyInvoice i) => new()
    {
        Id = i.Id,
        StudentName = i.Enrollment.Student.Name,
        GroupName = i.Enrollment.Group.Name,
        Year = i.Year,
        Month = i.Month,
        Amount = i.Amount,
        Discount = i.Discount,
        Paid = i.Payments.Sum(p => p.Amount),
        Status = i.Status,
        DueDate = i.DueDate,
        Payments = i.Payments.OrderBy(p => p.PaidAt)
            .Select(p => new PaymentRow { PaidAt = p.PaidAt, Amount = p.Amount, Method = p.Method, Note = p.Note }).ToList()
    };

    public async Task<List<InvoiceRow>> ListAsync(int year, int month, long groupId, int status)
    {
        var q = _u.MonthlyInvoice.GetAll(
            i => i.Year == year && i.Month == month
                 && (groupId == 0 || i.Enrollment.GroupId == groupId)
                 && (status < 0 || i.Status == (InvoiceStatus)status),
            "Enrollment.Student,Enrollment.Group,Payments");

        var list = await q.OrderBy(i => i.Enrollment.Student.Name).Take(500).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<InvoiceRow?> GetAsync(long id)
    {
        var i = await _u.MonthlyInvoice
            .GetAll(x => x.Id == id, "Enrollment.Student,Enrollment.Group,Payments").FirstOrDefaultAsync();
        return i == null ? null : Map(i);
    }

    public async Task<OpResult> GenerateAsync(int year, int month, long groupId)
    {
        var from = new DateTime(year, month, 1);
        var end = from.AddMonths(1).AddDays(-1);

        var enrollments = await _u.GroupEnrollment.GetAll(
            e => e.JoinedAt <= end && (e.LeftAt == null || e.LeftAt >= from)
                 && e.Group.IsActive && (groupId == 0 || e.GroupId == groupId), "Group").ToListAsync();

        var done = (await _u.MonthlyInvoice.GetAll(i => i.Year == year && i.Month == month)
            .Select(i => i.EnrollmentId).ToListAsync()).ToHashSet();

        var toAdd = enrollments.Where(e => !done.Contains(e.Id)).Select(e => new MonthlyInvoice
        {
            EnrollmentId = e.Id,
            Year = year,
            Month = month,
            Amount = e.CustomMonthlyFee ?? e.Group.MonthlyFee,
            DueDate = new DateTime(year, month, 10),
            Status = InvoiceStatus.Unpaid
        }).ToList();

        if (toAdd.Count == 0) return OpResult.Success("مفيش فواتير جديدة");
        _u.MonthlyInvoice.AddRange(toAdd);
        await _u.SaveAsync();
        return OpResult.Success($"تم إنشاء {toAdd.Count} فاتورة");
    }

    public async Task<OpResult> PayAsync(long invoiceId, decimal amount, decimal discount, PaymentMethod method, string? note)
    {
        var inv = await _u.MonthlyInvoice.GetAll(i => i.Id == invoiceId, "Payments", tracking: true).FirstOrDefaultAsync();
        if (inv == null) return OpResult.Fail("الفاتورة غير موجودة");
        if (discount < 0 || discount > inv.Amount) return OpResult.Fail("الخصم غير صحيح");
        if (amount < 0) return OpResult.Fail("المبلغ غير صحيح");

        var paid = inv.Payments.Sum(p => p.Amount);
        var due = inv.Amount - discount;
        if (paid + amount > due) return OpResult.Fail($"المبلغ أكبر من المتبقي ({due - paid:N2})");
        if (amount == 0 && discount == inv.Discount) return OpResult.Fail("اكتب المبلغ المدفوع أو غيّر الخصم");

        inv.Discount = discount;
        if (amount > 0)
            inv.Payments.Add(new Payment { Amount = amount, PaidAt = DateTime.Now, Method = method, Note = note });

        var total = paid + amount;
        inv.Status = total >= due ? InvoiceStatus.Paid : total > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;

        await _u.SaveAsync();
        return OpResult.Success("تم التحصيل");
    }
}