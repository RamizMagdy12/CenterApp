using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using Xunit;

public class BillingMathTests
{
    [Fact]
    public void Percentage_discount_of_20_on_1000_is_800_net()
    {
        var discount = BillingMath.DiscountAmount(1000m, DiscountKind.Percentage, 20m);
        Assert.Equal(200m, discount);
        Assert.Equal(800m, BillingMath.NetDue(1000m, discount));
        Assert.Equal(800m, BillingMath.Remaining(1000m, discount, 0m));
    }

    [Fact]
    public void Fixed_discount_and_rounding_stay_within_the_base_price()
    {
        Assert.Equal(200m, BillingMath.DiscountAmount(1000m, DiscountKind.Fixed, 200m));
        Assert.Equal(1000m, BillingMath.DiscountAmount(1000m, DiscountKind.Fixed, 1500m));
        Assert.Equal(10.01m, BillingMath.Money(10.005m));
        Assert.Equal(0m, BillingMath.DiscountAmount(1000m, DiscountKind.None, 20m));
    }

    [Fact]
    public void Status_follows_the_net_amount_and_payments()
    {
        Assert.Equal(InvoiceStatus.Unpaid, BillingMath.Status(800m, 0m));
        Assert.Equal(InvoiceStatus.PartiallyPaid, BillingMath.Status(800m, 100m));
        Assert.Equal(InvoiceStatus.Paid, BillingMath.Status(800m, 800m));
        Assert.Equal(InvoiceStatus.Paid, BillingMath.Status(0m, 0m));
    }
}

public class CollectionsTests
{
    [Fact]
    public async Task New_student_appears_in_collections_without_a_manual_invoice()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        var studentId = await lab.AddStudent("Mona Ali", groupId);

        var row = await lab.CurrentInvoice(studentId);
        Assert.Equal("Mona Ali", row.StudentName);
        Assert.Equal(1000m, row.Amount);
        Assert.Equal(0m, row.Discount);
        Assert.Equal(1000m, row.NetDue);
        Assert.Equal(1000m, row.Remaining);
        Assert.Equal(InvoiceStatus.Unpaid, row.Status);
        Assert.Equal(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 10), row.DueDate);

        await lab.Invoices.EnsureCurrentMonthForStudentAsync(studentId);
        var count = await lab.Invoices.ListAsync(DateTime.Today.Year, DateTime.Today.Month, 0, -1);
        Assert.Single(count);
    }

    [Fact]
    public async Task Enrollment_from_the_group_screen_creates_one_invoice()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Science", 1000m);
        var studentId = await lab.AddStudent("Omar Nabil");

        var added = await lab.Enrollments.AddAsync(groupId, studentId, 800m);
        Assert.True(added.Ok);

        var row = await lab.CurrentInvoice(studentId);
        Assert.Equal(800m, row.Amount);
        Assert.Equal(800m, row.Remaining);

        await lab.Invoices.EnsureMonthAsync(DateTime.Today.Year, DateTime.Today.Month);
        var rows = await lab.Invoices.ListAsync(DateTime.Today.Year, DateTime.Today.Month, 0, -1);
        Assert.Single(rows);
    }

    [Fact]
    public async Task Student_without_enrollment_or_with_zero_fee_is_not_billed()
    {
        await using var lab = await BillingLab.Open();
        var freeGroup = await lab.AddGroup("Free", 0m);
        await lab.AddStudent("No Group");
        var freeStudent = await lab.AddStudent("Free Student", freeGroup);

        var today = DateTime.Today;
        await lab.Invoices.EnsureMonthAsync(today.Year, today.Month);
        var rows = await lab.Invoices.ListAsync(today.Year, today.Month, 0, -1);
        Assert.DoesNotContain(rows, r => r.StudentId == freeStudent);
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Percentage_discount_applies_now_and_to_a_future_period_only()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        var studentId = await lab.AddStudent("Mona Ali", groupId);
        await lab.BackdateEnrollment(studentId);

        var past = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await lab.Invoices.EnsureMonthAsync(past.Year, past.Month, studentId: studentId);

        var saved = await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Percentage, 20m);
        Assert.True(saved.Ok, saved.Message);

        var current = await lab.CurrentInvoice(studentId);
        Assert.Equal(DiscountKind.Percentage, current.DiscountKind);
        Assert.Equal(20m, current.DiscountValue);
        Assert.Equal(200m, current.Discount);
        Assert.Equal(800m, current.NetDue);
        Assert.Equal(InvoiceStatus.Unpaid, current.Status);

        var pastRows = await lab.Invoices.ListAsync(past.Year, past.Month, 0, -1);
        var pastRow = Assert.Single(pastRows);
        Assert.Equal(0m, pastRow.Discount);
        Assert.Equal(1000m, pastRow.NetDue);

        var future = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        await lab.Invoices.EnsureMonthAsync(future.Year, future.Month, studentId: studentId);
        var futureRow = Assert.Single(await lab.Invoices.ListAsync(future.Year, future.Month, 0, -1));
        Assert.Equal(200m, futureRow.Discount);
        Assert.Equal(800m, futureRow.NetDue);
    }

    [Fact]
    public async Task Fixed_discount_is_snapshotted_and_a_later_change_keeps_paid_history()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        var studentId = await lab.AddStudent("Karim Adel", groupId);

        var saved = await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Fixed, 200m);
        Assert.True(saved.Ok, saved.Message);

        var current = await lab.CurrentInvoice(studentId);
        Assert.Equal(DiscountKind.Fixed, current.DiscountKind);
        Assert.Equal(200m, current.Discount);
        Assert.Equal(800m, current.NetDue);

        var partial = await lab.Invoices.PayAsync(current.Id, 300m, PaymentMethod.Cash, null);
        Assert.True(partial.Ok, partial.Message);

        var changed = await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Percentage, 10m);
        Assert.True(changed.Ok, changed.Message);

        current = await lab.Invoices.GetAsync(current.Id);
        Assert.NotNull(current);
        Assert.Equal(200m, current!.Discount);
        Assert.Equal(300m, current.Paid);
        Assert.Equal(500m, current.Remaining);
        Assert.Equal(InvoiceStatus.PartiallyPaid, current.Status);

        var removed = await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.None, 0m);
        Assert.True(removed.Ok, removed.Message);
        current = await lab.Invoices.GetAsync(current.Id);
        Assert.Equal(200m, current!.Discount);
        Assert.Equal(DiscountKind.None, current.DiscountKind);
    }

    [Fact]
    public async Task Partial_payment_then_final_payment_reaches_paid()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        var studentId = await lab.AddStudent("Mona Ali", groupId);
        await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Percentage, 20m);
        var row = await lab.CurrentInvoice(studentId);

        var partial = await lab.Invoices.PayAsync(row.Id, 300m, PaymentMethod.Cash, "first");
        Assert.True(partial.Ok, partial.Message);
        row = (await lab.Invoices.GetAsync(row.Id))!;
        Assert.Equal(500m, row.Remaining);
        Assert.Equal(InvoiceStatus.PartiallyPaid, row.Status);
        Assert.Equal("Cashier", row.Payments[0].RecordedBy);
        Assert.Equal(PaymentMethod.Cash, row.Payments[0].Method);
        Assert.NotNull(row.LastPaymentDate);

        var duplicate = await lab.Invoices.PayAsync(row.Id, 300m, PaymentMethod.Cash, "first");
        Assert.False(duplicate.Ok);

        var finalPay = await lab.Invoices.PayAsync(row.Id, 500m, PaymentMethod.Transfer, null);
        Assert.True(finalPay.Ok, finalPay.Message);
        row = (await lab.Invoices.GetAsync(row.Id))!;
        Assert.Equal(0m, row.Remaining);
        Assert.Equal(800m, row.Paid);
        Assert.Equal(InvoiceStatus.Paid, row.Status);
        Assert.Equal(2, row.Payments.Count);
    }

    [Fact]
    public async Task Invalid_discounts_and_payments_are_rejected()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        var studentId = await lab.AddStudent("Mona Ali", groupId);
        var row = await lab.CurrentInvoice(studentId);

        Assert.False((await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Percentage, 120m)).Ok);
        Assert.False((await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Percentage, -5m)).Ok);
        Assert.False((await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Fixed, 1000.01m)).Ok);
        Assert.False((await lab.Invoices.SaveDiscountAsync(studentId, DiscountKind.Fixed, -1m)).Ok);
        Assert.False((await lab.Invoices.PayAsync(row.Id, -10m, PaymentMethod.Cash, null)).Ok);
        Assert.False((await lab.Invoices.PayAsync(row.Id, 0m, PaymentMethod.Wallet, null)).Ok);
        Assert.False((await lab.Invoices.PayAsync(row.Id, 1000.01m, PaymentMethod.Transfer, null)).Ok);

        row = (await lab.Invoices.GetAsync(row.Id))!;
        Assert.Equal(0m, row.Discount);
        Assert.Equal(0m, row.Paid);
        Assert.Equal(InvoiceStatus.Unpaid, row.Status);
        Assert.Empty(row.Payments);
    }

    [Fact]
    public async Task Search_filters_by_full_name()
    {
        await using var lab = await BillingLab.Open();
        var groupId = await lab.AddGroup("Math", 1000m);
        await lab.AddStudent("Mona Ali", groupId);
        await lab.AddStudent("Karim Adel", groupId);

        var today = DateTime.Today;
        var found = await lab.Invoices.ListAsync(today.Year, today.Month, 0, -1, "mona");
        var row = Assert.Single(found);
        Assert.Equal("Mona Ali", row.StudentName);
    }
}
