using CenterApp.Entity.Center;
using CenterApp.Service.Models;

public interface IInvoiceService
{
    Task<List<InvoiceRow>> ListAsync(int year, int month, long groupId, int status, string? search = null, string? sort = null);
    Task<InvoiceRow?> GetAsync(long id);
    Task<int> EnsureMonthAsync(int year, int month, long groupId = 0, long studentId = 0);
    Task EnsureCurrentMonthForStudentAsync(long studentId);
    Task<OpResult> GenerateAsync(int year, int month, long groupId);
    Task<OpResult> PayAsync(long invoiceId, decimal amount, PaymentMethod method, string? note);
    Task<DiscountFormVm?> GetDiscountAsync(long studentId);
    Task<OpResult> SaveDiscountAsync(long studentId, DiscountKind kind, decimal value);
}
