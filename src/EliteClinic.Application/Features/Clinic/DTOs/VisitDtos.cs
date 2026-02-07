using EliteClinic.Domain.Enums;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Visit DTOs ────────────────────────────────────────────────────

public class VisitDto
{
    public Guid Id { get; set; }
    public Guid? QueueTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public VisitStatus Status { get; set; }
    public string? Complaint { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }

    // Vitals
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

    public List<PrescriptionDto> Prescriptions { get; set; } = new();
    public List<LabRequestDto> LabRequests { get; set; } = new();
    public InvoiceDto? Invoice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVisitRequest
{
    public Guid? QueueTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public string? Complaint { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVisitRequest
{
    public string? Complaint { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }

    // Vitals
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
}

public class CompleteVisitRequest
{
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
}

// ─── Patient Summary DTO ───────────────────────────────────────────

public class PatientSummaryDto
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
    public List<VisitSummaryDto> RecentVisits { get; set; } = new();
}

public class VisitSummaryDto
{
    public Guid Id { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? Complaint { get; set; }
    public string? Diagnosis { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
