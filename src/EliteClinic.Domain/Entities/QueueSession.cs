namespace EliteClinic.Domain.Entities;

/// <summary>
/// A queue session represents a shift/time-block where patients are seen.
/// Default: one active session per tenant per day.
/// Optionally doctor-scoped if DoctorId is provided.
/// </summary>
public class QueueSession : TenantBaseEntity
{
    public Guid? DoctorId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    public Doctor? Doctor { get; set; }
    public ICollection<QueueTicket> Tickets { get; set; } = new List<QueueTicket>();

    public QueueSession()
    {
        IsActive = true;
        StartedAt = DateTime.UtcNow;
    }
}
