using CenterApp.Entity.Center;
using CenterApp.Entity.Identity;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;



public class TransferService : ITransferService
{
    private readonly IUnitOfWork _u;
    private readonly UserManager<AppUser> _um;
    private readonly IInvoiceService _invoices;

    public TransferService(IUnitOfWork u, UserManager<AppUser> um, IInvoiceService invoices)
    { _u = u; _um = um; _invoices = invoices; }

    // المجموعات المتاحة للاختيار (مع الأماكن الفاضية والكود الجاي). studentId = null عند التسجيل.
    public async Task<List<GroupPick>> PicksAsync(long? studentId)
    {
        var counts = (await _u.GroupEnrollment.GetAll(e => e.IsActive)
            .GroupBy(e => e.GroupId).Select(g => new { g.Key, C = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.C);

        var mine = studentId.HasValue
            ? (await _u.GroupEnrollment.GetAll(e => e.StudentId == studentId && e.IsActive)
                .Select(e => e.GroupId).ToListAsync()).ToHashSet()
            : new HashSet<long>();

        var groups = await _u.Group.GetAll(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();

        return groups.Where(g => !mine.Contains(g.Id)).Select(g =>
        {
            counts.TryGetValue(g.Id, out var taken);
            var free = g.Capacity > 0 ? g.Capacity - taken : -1;
            var hasCode = !string.IsNullOrWhiteSpace(g.Code);
            string? why = !hasCode ? "لازم تحط لها كود الأول" : free == 0 ? "مكتملة العدد" : null;

            return new GroupPick
            {
                Id = g.Id,
                Name = g.Name,
                Code = g.Code ?? "",
                Free = free,
                NextCode = hasCode ? $"{g.LastStudentNumber + 1:D3}{g.Code}" : "",
                Disabled = why != null,
                Why = why
            };
        }).ToList();
    }

    public async Task<TransferFormVm?> FormAsync(long studentId, long? from)
    {
        var s = await _u.Student.GetAll(x => x.Id == studentId).FirstOrDefaultAsync();
        if (s == null) return null;

        var current = await _u.GroupEnrollment.GetAll(e => e.StudentId == studentId && e.IsActive, "Group")
            .Select(e => new { e.GroupId, e.Group.Name, e.Group.Code }).ToListAsync();

        return new TransferFormVm
        {
            StudentId = s.Id,
            StudentName = s.Name,
            Code = s.Code ?? "",
            CodeGroupId = s.CodeGroupId ?? 0,
            FromGroupId = from ?? (current.Count == 1 ? current[0].GroupId : null),
            Current = current.Select(c => new SimpleItem(c.GroupId,
                string.IsNullOrEmpty(c.Code) ? c.Name : $"{c.Name} ({c.Code})")).ToList(),
            Targets = await PicksAsync(studentId)
        };
    }

    public async Task<OpResult> TransferAsync(long studentId, long fromGroupId, long toGroupId)
    {
        if (fromGroupId == toGroupId) return OpResult.Fail("اختار مجموعة تانية غير الحالية");

        var student = await _u.Student.GetAll(s => s.Id == studentId, tracking: true).FirstOrDefaultAsync();
        if (student == null) return OpResult.Fail("الطالب غير موجود");

        var oldEnr = await _u.GroupEnrollment
            .GetAll(e => e.StudentId == studentId && e.GroupId == fromGroupId && e.IsActive, "Group", tracking: true)
            .FirstOrDefaultAsync();
        if (oldEnr == null) return OpResult.Fail("الطالب مش مسجل في المجموعة دي");

        var target = await _u.Group.GetAll(g => g.Id == toGroupId && g.IsActive, tracking: true).FirstOrDefaultAsync();
        if (target == null) return OpResult.Fail("المجموعة الجديدة غير موجودة أو متوقفة");
        if (string.IsNullOrWhiteSpace(target.Code))
            return OpResult.Fail("المجموعة الجديدة ملهاش كود، افتحها من شاشة المجموعات وحط لها كود الأول");

        if (await _u.GroupEnrollment.GetAll(e => e.StudentId == studentId && e.GroupId == toGroupId && e.IsActive).AnyAsync())
            return OpResult.Fail("الطالب مسجل في المجموعة الجديدة بالفعل");

        var taken = await _u.GroupEnrollment.GetAll(e => e.GroupId == toGroupId && e.IsActive).CountAsync();
        if (target.Capacity > 0 && taken >= target.Capacity)
            return OpResult.Fail("المجموعة الجديدة مكتملة العدد");

        var today = DateTime.Today;
        int y = today.Year, m = today.Month;

        // ── التسجيل
        oldEnr.IsActive = false;
        oldEnr.LeftAt = today;

        var newEnr = new GroupEnrollment { GroupId = toGroupId, StudentId = studentId, JoinedAt = today, IsActive = true };
        _u.GroupEnrollment.Add(newEnr);

        // ── الكود (الـ QR ثابت: التوكن بيتحفظ قبل أي تغيير)
        var oldCode = student.Code;
        var recode = student.CodeGroupId == null || student.CodeGroupId == fromGroupId;
        if (recode)
        {
            student.QrToken ??= student.Code;
            target.LastStudentNumber++;
            student.Code = $"{target.LastStudentNumber:D3}{target.Code}";
            student.CodeGroupId = toGroupId;
        }

        _u.StudentTransfer.Add(new StudentTransfer
        {
            StudentId = studentId,
            FromGroupId = fromGroupId,
            ToGroupId = toGroupId,
            OldCode = oldCode,
            NewCode = student.Code,
            TransferDate = today
        });

        // ── فاتورة الشهر الحالي: واحدة بس هي اللي تتحاسب
        void AddWaived(GroupEnrollment e, decimal price)
        {
            price = Math.Round(price, 2);
            if (price <= 0) return;
            _u.MonthlyInvoice.Add(new MonthlyInvoice
            {
                Enrollment = e,
                Year = y,
                Month = m,
                Amount = price,
                Discount = price,
                DueDate = new DateTime(y, m, 10),
                Status = InvoiceStatus.Waived
            });
        }

        var oldInvoice = await _u.MonthlyInvoice
            .GetAll(i => i.EnrollmentId == oldEnr.Id && i.Year == y && i.Month == m, "Payments", tracking: true)
            .FirstOrDefaultAsync();

        string billing;
        if (oldInvoice != null && oldInvoice.Payments.Count > 0)
        {
            AddWaived(newEnr, target.MonthlyFee);
            billing = "الشهر الحالي اتحاسب في المجموعة القديمة، والجديدة بتتحاسب من الشهر الجاي.";
        }
        else
        {
            if (oldInvoice != null)
            {
                oldInvoice.Discount = oldInvoice.Amount;
                oldInvoice.Status = InvoiceStatus.Waived;
            }
            else AddWaived(oldEnr, oldEnr.CustomMonthlyFee ?? oldEnr.Group.MonthlyFee);

            billing = "فاتورة الشهر الحالي اتنقلت للمجموعة الجديدة بسعرها.";
        }

        try { await _u.SaveInTransactionAsync(); }
        catch (DbUpdateConcurrencyException) { return OpResult.Fail("حصل تعديل في نفس اللحظة على المجموعة، جرّب تاني"); }

        // الفاتورة العادية للمجموعة الجديدة (لو لسه ما اتعملتش)
        await _invoices.EnsureCurrentMonthForStudentAsync(studentId);

        // اسم دخول الطالب = كوده
        string? warn = null;
        if (recode && !string.IsNullOrEmpty(student.UserId))
        {
            var user = await _um.FindByIdAsync(student.UserId);
            if (user != null)
            {
                var r = await _um.SetUserNameAsync(user, student.Code!);
                if (!r.Succeeded) warn = "تعذر تغيير اسم دخول الطالب لكوده الجديد، غيّره من حساب الدخول.";
            }
        }

        var msg = $"تم نقل {student.Name} من \"{oldEnr.Group.Name}\" إلى \"{target.Name}\". ";
        msg += recode
            ? $"كود الطالب الجديد: {student.Code} (كان {oldCode}). الـ QR القديم لسه شغال. "
            : $"كود الطالب زي ما هو ({student.Code}). ";
        msg += billing;
        if (recode && !string.IsNullOrEmpty(student.UserId) && warn == null) msg += " اسم دخول الطالب اتغيّر لكوده الجديد.";
        if (warn != null) msg += " " + warn;

        return new OpResult(true, msg, student.Id, student.Code);
    }
}