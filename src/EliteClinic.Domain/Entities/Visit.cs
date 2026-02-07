using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

/// <summary>
/// A clinical visit. Can be created from a queue ticket OR manually.
/// Contains vitals, complaint, diagnosis, notes, follow-up date.
/// </summary>
public class Visit : TenantBaseEntity
{
    public Guid? QueueTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public VisitStatus Status { get; set; }
    public string? Complaint { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }

    // Vitals (nullable — filled based on DoctorVisitFieldConfig)
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? BMI { get; set; }
    public decimal? BloodSugar { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public int? RespiratoryRate { get; set; }

    public DateTime? FollowUpDate { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Doctor Doctor { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
    public QueueTicket? QueueTicket { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();

    public Visit()
    {
        Status = VisitStatus.Open;
        StartedAt = DateTime.UtcNow;
    }
}
