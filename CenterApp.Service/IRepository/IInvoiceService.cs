using CenterApp.Entity.Center;
using CenterApp.Service.Models;

public interface IInvoiceService
{
    Task<List<InvoiceRow>> ListAsync(int year, int month, long groupId, int status);
    Task<InvoiceRow?> GetAsync(long id);
    Task<OpResult> GenerateAsync(int year, int month, long groupId);
    Task<OpResult> PayAsync(long invoiceId, decimal amount, decimal discount, PaymentMethod method, string? note);
}