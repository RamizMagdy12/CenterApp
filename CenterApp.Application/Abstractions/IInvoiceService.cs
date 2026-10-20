using CenterApp.Domain.Center;
using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IInvoiceService
{
    Task<List<InvoiceRow>> ListAsync(int year, int month, long groupId, int status, string? search = null, string? sort = null);
    Task<InvoiceRow?> GetAsync(long id);
    Task<int> EnsureMonthAsync(int year, int month, long groupId = 0, long studentId = 0);
    Task EnsureCurrentMonthForStudentAsync(long studentId);
    Task<OpResult> GenerateAsync(int year, int month, long groupId);
    Task<OpResult> PayAsync(long invoiceId, decimal amount, PaymentMethod method, string? note, string? noteEn = null);
    Task<DiscountFormVm?> GetDiscountAsync(long studentId);
    Task<OpResult> SaveDiscountAsync(long studentId, DiscountKind kind, decimal value);
    Task<OpResult> RecalculateDiscountsAsync(int year, int month, long groupId);
    Task<OpResult> UpdatePaymentAsync(long paymentId, decimal amount, DateTime? paidAt, PaymentMethod method, string? note, string? noteEn = null);
    Task<OpResult> DeletePaymentAsync(long paymentId);
}
