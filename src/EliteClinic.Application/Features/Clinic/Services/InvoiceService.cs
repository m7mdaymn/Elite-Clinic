using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class InvoiceService : IInvoiceService
{
    private readonly EliteClinicDbContext _context;

    public InvoiceService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(Guid tenantId, CreateInvoiceRequest request)
    {
        var visit = await _context.Visits
            .Include(v => v.Doctor)
            .Include(v => v.Patient)
            .FirstOrDefaultAsync(v => v.Id == request.VisitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<InvoiceDto>.Error("Visit not found");

        // Check no invoice already exists for this visit
        var existing = await _context.Invoices
            .FirstOrDefaultAsync(i => i.VisitId == request.VisitId && !i.IsDeleted);
        if (existing != null)
            return ApiResponse<InvoiceDto>.Error("An invoice already exists for this visit");

        if (request.Amount <= 0)
            return ApiResponse<InvoiceDto>.Error("Amount must be greater than zero");

        var invoice = new Invoice
        {
            TenantId = tenantId,
            VisitId = request.VisitId,
            PatientId = visit.PatientId,
            DoctorId = visit.DoctorId,
            Amount = request.Amount,
            PaidAmount = 0,
            RemainingAmount = request.Amount,
            Status = InvoiceStatus.Unpaid,
            Notes = request.Notes
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var saved = await GetInvoiceWithIncludes(tenantId, invoice.Id);
        return ApiResponse<InvoiceDto>.Created(MapToDto(saved!), "Invoice created successfully");
    }

    public async Task<ApiResponse<InvoiceDto>> UpdateInvoiceAsync(Guid tenantId, Guid invoiceId, UpdateInvoiceRequest request)
    {
        var invoice = await GetInvoiceWithIncludes(tenantId, invoiceId);
        if (invoice == null)
            return ApiResponse<InvoiceDto>.Error("Invoice not found");

        // Check parent visit is still open
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == invoice.VisitId && !v.IsDeleted);
        if (visit != null && visit.Status != VisitStatus.Open)
            return ApiResponse<InvoiceDto>.Error("Cannot update invoice — visit is already completed");

        if (request.Amount <= 0)
            return ApiResponse<InvoiceDto>.Error("Amount must be greater than zero");

        if (request.Amount < invoice.PaidAmount)
            return ApiResponse<InvoiceDto>.Error("New amount cannot be less than already paid amount");

        invoice.Amount = request.Amount;
        invoice.RemainingAmount = request.Amount - invoice.PaidAmount;
        invoice.Notes = request.Notes;

        // Recalculate status
        invoice.Status = invoice.RemainingAmount <= 0 ? InvoiceStatus.Paid
            : invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid
            : InvoiceStatus.Unpaid;

        await _context.SaveChangesAsync();

        var updated = await GetInvoiceWithIncludes(tenantId, invoiceId);
        return ApiResponse<InvoiceDto>.Ok(MapToDto(updated!), "Invoice updated successfully");
    }

    public async Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid tenantId, Guid invoiceId)
    {
        var invoice = await GetInvoiceWithIncludes(tenantId, invoiceId);
        if (invoice == null)
            return ApiResponse<InvoiceDto>.Error("Invoice not found");

        return ApiResponse<InvoiceDto>.Ok(MapToDto(invoice), "Invoice retrieved successfully");
    }

    public async Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(Guid tenantId, DateTime? from, DateTime? to,
        Guid? doctorId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Doctor)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .Where(i => i.TenantId == tenantId && !i.IsDeleted);

        if (from.HasValue)
            query = query.Where(i => i.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(i => i.CreatedAt <= to.Value.AddDays(1));
        if (doctorId.HasValue)
            query = query.Where(i => i.DoctorId == doctorId.Value);

        var totalCount = await query.CountAsync();
        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<InvoiceDto>
        {
            Items = invoices.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<InvoiceDto>>.Ok(result, $"Retrieved {result.Items.Count} invoice(s)");
    }

    public async Task<ApiResponse<PaymentDto>> RecordPaymentAsync(Guid tenantId, CreatePaymentRequest request)
    {
        var invoice = await GetInvoiceWithIncludes(tenantId, request.InvoiceId);
        if (invoice == null)
            return ApiResponse<PaymentDto>.Error("Invoice not found");

        if (request.Amount <= 0)
            return ApiResponse<PaymentDto>.Error("Payment amount must be greater than zero");

        if (request.Amount > invoice.RemainingAmount)
            return ApiResponse<PaymentDto>.Error($"Payment amount ({request.Amount}) exceeds remaining amount ({invoice.RemainingAmount})");

        var payment = new Payment
        {
            TenantId = tenantId,
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            PaidAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        // Update invoice totals
        invoice.PaidAmount += request.Amount;
        invoice.RemainingAmount = invoice.Amount - invoice.PaidAmount;
        invoice.Status = invoice.RemainingAmount <= 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

        await _context.SaveChangesAsync();

        return ApiResponse<PaymentDto>.Created(new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            PaidAt = payment.PaidAt,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        }, "Payment recorded successfully");
    }

    public async Task<ApiResponse<List<PaymentDto>>> GetPaymentsByInvoiceAsync(Guid tenantId, Guid invoiceId)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId && !i.IsDeleted);
        if (invoice == null)
            return ApiResponse<List<PaymentDto>>.Error("Invoice not found");

        var payments = await _context.Payments
            .Where(p => p.InvoiceId == invoiceId && p.TenantId == tenantId && !p.IsDeleted)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        return ApiResponse<List<PaymentDto>>.Ok(
            payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                PaidAt = p.PaidAt,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt
            }).ToList(),
            $"Retrieved {payments.Count} payment(s)");
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<Invoice?> GetInvoiceWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.Doctor)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId && !i.IsDeleted);
    }

    private static InvoiceDto MapToDto(Invoice i)
    {
        return new InvoiceDto
        {
            Id = i.Id,
            VisitId = i.VisitId,
            PatientId = i.PatientId,
            PatientName = i.Patient?.Name ?? string.Empty,
            DoctorId = i.DoctorId,
            DoctorName = i.Doctor?.Name ?? string.Empty,
            Amount = i.Amount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            Status = i.Status,
            Notes = i.Notes,
            Payments = i.Payments?.Where(p => !p.IsDeleted).Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                PaidAt = p.PaidAt,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt
            }).ToList() ?? new(),
            CreatedAt = i.CreatedAt
        };
    }
}
