using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

/// <summary>
/// ONE invoice per visit. Tracks total amount, paid amount, and remaining.
/// Editable only while the parent Visit status is Open.
/// </summary>
public class Invoice : TenantBaseEntity
{
    public Guid VisitId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }

    public Visit Visit { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public Invoice()
    {
        Status = InvoiceStatus.Unpaid;
        PaidAmount = 0;
    }
}
