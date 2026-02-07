namespace EliteClinic.Domain.Entities;

/// <summary>
/// An individual payment transaction against an Invoice.
/// Partial payments are allowed. Amount cannot exceed Invoice.RemainingAmount.
/// </summary>
public class Payment : TenantBaseEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public Payment()
    {
        PaidAt = DateTime.UtcNow;
    }
}
