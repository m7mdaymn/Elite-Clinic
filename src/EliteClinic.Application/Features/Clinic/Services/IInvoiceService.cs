using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(Guid tenantId, CreateInvoiceRequest request);
    Task<ApiResponse<InvoiceDto>> UpdateInvoiceAsync(Guid tenantId, Guid invoiceId, UpdateInvoiceRequest request);
    Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId);
    Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(Guid tenantId, DateTime? from, DateTime? to, Guid? doctorId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<PaymentDto>> RecordPaymentAsync(Guid tenantId, CreatePaymentRequest request);
    Task<ApiResponse<List<PaymentDto>>> GetPaymentsByInvoiceAsync(Guid tenantId, Guid invoiceId);
}
