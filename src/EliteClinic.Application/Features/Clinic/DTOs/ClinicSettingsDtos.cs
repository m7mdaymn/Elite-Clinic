using System.ComponentModel.DataAnnotations;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ───── Clinic Settings DTOs ─────

public class ClinicSettingsDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsAppSenderNumber { get; set; }
    public string? SupportWhatsAppNumber { get; set; }
    public string? SupportPhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? LogoUrl { get; set; }
    public bool BookingEnabled { get; set; }
    public int CancellationWindowHours { get; set; }
    public List<WorkingHourDto> WorkingHours { get; set; } = new();
}

public class UpdateClinicSettingsRequest
{
    [Required(ErrorMessage = "ClinicName is required")]
    [StringLength(200, ErrorMessage = "ClinicName cannot exceed 200 characters")]
    public string ClinicName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string? Phone { get; set; }

    [StringLength(20)]
    public string? WhatsAppSenderNumber { get; set; }

    [StringLength(20)]
    public string? SupportWhatsAppNumber { get; set; }

    [StringLength(20)]
    public string? SupportPhoneNumber { get; set; }

    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public string? LogoUrl { get; set; }
    public bool BookingEnabled { get; set; }

    [Range(0, 168, ErrorMessage = "CancellationWindowHours must be between 0 and 168")]
    public int CancellationWindowHours { get; set; } = 2;

    public List<WorkingHourRequest>? WorkingHours { get; set; }
}

public class WorkingHourDto
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class WorkingHourRequest
{
    [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 (Sunday) and 6 (Saturday)")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required(ErrorMessage = "StartTime is required")]
    public string StartTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "EndTime is required")]
    public string EndTime { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
