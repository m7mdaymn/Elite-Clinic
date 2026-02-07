using EliteClinic.Domain.Enums;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Invoice DTOs ──────────────────────────────────────────────────

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<PaymentDto> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateInvoiceRequest
{
    public Guid VisitId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInvoiceRequest
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

// ─── Payment DTOs ──────────────────────────────────────────────────

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
