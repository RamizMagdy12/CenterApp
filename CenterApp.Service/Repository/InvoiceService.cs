using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.EntityFrameworkCore;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _u;
    public InvoiceService(IUnitOfWork u) => _u = u;

    private static InvoiceRow Map(MonthlyInvoice i)
    {
        var payments = i.Payments.OrderBy(p => p.PaidAt).ToList();
        return new InvoiceRow
        {
            Id = i.Id,
            StudentId = i.Enrollment.StudentId,
            StudentName = i.Enrollment.Student.Name,
            GroupName = i.Enrollment.Group.Name,
            Year = i.Year,
            Month = i.Month,
            Amount = i.Amount,
            Discount = i.Discount,
            DiscountKind = i.Enrollment.Student.DiscountKind,
            DiscountValue = i.Enrollment.Student.DiscountValue,
            Paid = payments.Sum(p => p.Amount),
            Status = i.Status,
            DueDate = i.DueDate,
            LastPaymentDate = payments.Count == 0 ? null : payments.Max(p => p.PaidAt),
            Payments = payments.Select(p => new PaymentRow
            {
                PaidAt = p.PaidAt,
                Amount = p.Amount,
                Method = p.Method,
                Note = p.Note,
                RecordedBy = p.CreatedByName
            }).ToList()
        };
    }

    public async Task<List<InvoiceRow>> ListAsync(int year, int month, long groupId, int status, string? search = null, string? sort = null)
    {
        var q = _u.MonthlyInvoice.GetAll(
            i => i.Year == year && i.Month == month
                 && (groupId == 0 || i.Enrollment.GroupId == groupId)
                 && (status < 0 || i.Status == (InvoiceStatus)status),
            "Enrollment.Student,Enrollment.Group,Payments");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(i => i.Enrollment.Student.Name.ToLower().Contains(term));
        }

        var rows = (await q.ToListAsync()).Select(Map).ToList();
        rows = sort switch
        {
            "balance-asc" => rows.OrderBy(r => r.Remaining).ThenBy(r => r.StudentName).ToList(),
            "due-asc" => rows.OrderBy(r => r.DueDate).ThenBy(r => r.StudentName).ToList(),
            "due-desc" => rows.OrderByDescending(r => r.DueDate).ThenBy(r => r.StudentName).ToList(),
            "balance-desc" => rows.OrderByDescending(r => r.Remaining).ThenBy(r => r.StudentName).ToList(),
            _ => rows.OrderBy(r => r.StudentName).ThenBy(r => r.GroupName).ToList()
        };
        return rows.Take(500).ToList();
    }

    public async Task<InvoiceRow?> GetAsync(long id)
    {
        var i = await _u.MonthlyInvoice
            .GetAll(x => x.Id == id, "Enrollment.Student,Enrollment.Group,Payments").FirstOrDefaultAsync();
        return i == null ? null : Map(i);
    }

    public Task EnsureCurrentMonthForStudentAsync(long studentId)
    {
        var today = DateTime.Today;
        return EnsureMonthAsync(today.Year, today.Month, studentId: studentId);
    }

    public async Task<OpResult> GenerateAsync(int year, int month, long groupId)
    {
        var added = await EnsureMonthAsync(year, month, groupId);
        return added == 0
            ? OpResult.Success("لا توجد فواتير جديدة")
            : OpResult.Success($"تم إنشاء {added} فاتورة");
    }

    public async Task<int> EnsureMonthAsync(int year, int month, long groupId = 0, long studentId = 0)
    {
        var from = new DateTime(year, month, 1);
        var end = from.AddMonths(1).AddDays(-1);

        var enrollments = await _u.GroupEnrollment.GetAll(
            e => e.JoinedAt <= end && (e.LeftAt == null || e.LeftAt >= from)
                 && e.Group.IsActive
                 && (groupId == 0 || e.GroupId == groupId)
                 && (studentId == 0 || e.StudentId == studentId),
            "Group,Student").ToListAsync();

        var done = (await _u.MonthlyInvoice.GetAll(i => i.Year == year && i.Month == month)
            .Select(i => i.EnrollmentId).ToListAsync()).ToHashSet();

        var toAdd = new List<MonthlyInvoice>();
        foreach (var e in enrollments)
        {
            if (done.Contains(e.Id)) continue;
            var invoice = BuildInvoice(e, year, month);
            if (invoice != null) toAdd.Add(invoice);
        }

        if (toAdd.Count == 0) return 0;

        _u.MonthlyInvoice.AddRange(toAdd);
        try
        {
            await _u.SaveInTransactionAsync();
            return toAdd.Count;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            foreach (var inv in toAdd) _u.Detach(inv);
            return 0;
        }
    }

    public async Task<OpResult> PayAsync(long invoiceId, decimal amount, PaymentMethod method, string? note)
    {
        if (!Enum.IsDefined(method)) return OpResult.Fail("طريقة الدفع غير صحيحة");

        var money = BillingMath.Money(amount);
        if (money <= 0) return OpResult.Fail("مبلغ الدفعة يجب أن يكون أكبر من صفر");
        var inv = await _u.MonthlyInvoice.GetAll(i => i.Id == invoiceId, "Payments", tracking: true).FirstOrDefaultAsync();
        if (inv == null) return OpResult.Fail("الفاتورة غير موجودة");

        var paid = inv.Payments.Sum(p => p.Amount);
        var remaining = BillingMath.Remaining(inv.Amount, inv.Discount, paid);
        if (money > remaining)
            return OpResult.Fail($"المبلغ أكبر من المتبقي ({remaining:N2} ج.م)");

        var now = DateTime.Now;
        if (inv.Payments.Any(p => p.Amount == money && p.Method == method && p.PaidAt >= now.AddSeconds(-15)))
            return OpResult.Fail("هذه الدفعة مسجلة بالفعل");

        inv.Payments.Add(new Payment
        {
            Amount = money,
            PaidAt = now,
            Method = method,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
        inv.Status = BillingMath.Status(BillingMath.NetDue(inv.Amount, inv.Discount), paid + money);

        await _u.SaveInTransactionAsync();
        return OpResult.Success("تم تسجيل الدفعة");
    }

    public async Task<DiscountFormVm?> GetDiscountAsync(long studentId)
    {
        var student = await _u.Student.GetAll(s => s.Id == studentId).FirstOrDefaultAsync();
        if (student == null) return null;

        return new DiscountFormVm
        {
            StudentId = student.Id,
            StudentName = student.Name,
            Kind = student.DiscountKind,
            Value = student.DiscountValue,
            BasePrices = await ActiveBasePricesAsync(studentId)
        };
    }

    public async Task<OpResult> SaveDiscountAsync(long studentId, DiscountKind kind, decimal value)
    {
        var student = await _u.Student.GetAll(s => s.Id == studentId, tracking: true).FirstOrDefaultAsync();
        if (student == null) return OpResult.Fail("الطالب غير موجود");

        if (kind == DiscountKind.None)
            value = 0;
        else if (!Enum.IsDefined(kind))
            return OpResult.Fail("نوع الخصم غير صحيح");
        else if (kind == DiscountKind.Percentage)
        {
            value = BillingMath.Money(value);
            if (value <= 0 || value > 100)
                return OpResult.Fail("نسبة الخصم يجب أن تكون أكبر من 0 ولا تتجاوز 100");
        }
        else if (kind == DiscountKind.Fixed)
        {
            value = BillingMath.Money(value);
            if (value <= 0)
                return OpResult.Fail("الخصم الثابت يجب أن يكون أكبر من صفر");
            var bases = await ActiveBasePricesAsync(studentId);
            if (bases.Any(b => value > b))
                return OpResult.Fail("الخصم الثابت لا يمكن أن يتجاوز سعر الاشتراك");
        }

        student.DiscountKind = kind;
        student.DiscountValue = value;
        await RefreshOpenInvoicesAsync(student, kind, value);
        await _u.SaveInTransactionAsync();
        return OpResult.Success(kind == DiscountKind.None ? "تم إزالة الخصم" : "تم حفظ الخصم");
    }

    private async Task RefreshOpenInvoicesAsync(Student student, DiscountKind kind, decimal value)
    {
        var today = DateTime.Today;
        var invoices = await _u.MonthlyInvoice.GetAll(
            i => i.Enrollment.StudentId == student.Id
                 && i.Year == today.Year && i.Month == today.Month
                 && i.Status == InvoiceStatus.Unpaid,
            "Payments",
            tracking: true).ToListAsync();

        foreach (var inv in invoices)
        {
            if (inv.Payments.Count > 0) continue;
            var discount = BillingMath.DiscountAmount(inv.Amount, kind, value);
            inv.Discount = discount;
            inv.Status = BillingMath.Status(BillingMath.NetDue(inv.Amount, discount), 0);
        }
    }

    private async Task<List<decimal>> ActiveBasePricesAsync(long studentId)
    {
        return await _u.GroupEnrollment.GetAll(
                e => e.StudentId == studentId && e.IsActive && e.LeftAt == null && e.Group.IsActive,
                "Group")
            .Select(e => e.CustomMonthlyFee ?? e.Group.MonthlyFee)
            .Where(fee => fee > 0)
            .ToListAsync();
    }

    private static MonthlyInvoice? BuildInvoice(GroupEnrollment e, int year, int month)
    {
        var basePrice = BillingMath.Money(e.CustomMonthlyFee ?? e.Group.MonthlyFee);
        if (basePrice <= 0) return null;

        var discount = BillingMath.DiscountAmount(basePrice, e.Student.DiscountKind, e.Student.DiscountValue);
        return new MonthlyInvoice
        {
            EnrollmentId = e.Id,
            Year = year,
            Month = month,
            Amount = basePrice,
            Discount = discount,
            DueDate = new DateTime(year, month, 10),
            Status = BillingMath.Status(BillingMath.NetDue(basePrice, discount), 0)
        };
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
