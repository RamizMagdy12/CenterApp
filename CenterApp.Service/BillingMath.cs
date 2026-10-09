using CenterApp.Entity.Center;

namespace CenterApp.Service.Models;

public static class BillingMath
{
    public static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal DiscountAmount(decimal basePrice, DiscountKind kind, decimal value)
    {
        if (basePrice <= 0 || kind == DiscountKind.None || value <= 0)
            return 0;

        var raw = kind == DiscountKind.Percentage ? basePrice * value / 100m : value;
        var amount = Money(raw);
        if (amount < 0) return 0;
        var cap = Money(basePrice);
        return amount > cap ? cap : amount;
    }

    public static decimal NetDue(decimal basePrice, decimal discount) => Money(basePrice - discount);

    public static decimal Remaining(decimal basePrice, decimal discount, decimal paid)
        => Money(NetDue(basePrice, discount) - paid);

    public static InvoiceStatus Status(decimal netDue, decimal paid)
    {
        var net = Money(netDue);
        var total = Money(paid);
        if (net <= 0 || total >= net) return InvoiceStatus.Paid;
        if (total > 0) return InvoiceStatus.PartiallyPaid;
        return InvoiceStatus.Unpaid;
    }
}
