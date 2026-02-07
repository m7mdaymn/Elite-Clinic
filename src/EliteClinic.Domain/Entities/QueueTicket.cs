using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

/// <summary>
/// A queue ticket for a patient waiting to see a doctor within a session.
/// </summary>
public class QueueTicket : TenantBaseEntity
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? DoctorServiceId { get; set; }
    public int TicketNumber { get; set; }
    public TicketStatus Status { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? VisitStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? SkippedAt { get; set; }
    public string? Notes { get; set; }

    public QueueSession Session { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public DoctorService? DoctorService { get; set; }

    public QueueTicket()
    {
        Status = TicketStatus.Waiting;
        IsUrgent = false;
        IssuedAt = DateTime.UtcNow;
    }
}
