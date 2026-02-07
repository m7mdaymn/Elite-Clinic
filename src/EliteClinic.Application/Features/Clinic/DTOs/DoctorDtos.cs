using EliteClinic.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ───── Doctor DTOs ─────

public class DoctorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsEnabled { get; set; }
    public string Username { get; set; } = string.Empty;
    public UrgentCaseMode UrgentCaseMode { get; set; }
    public int AvgVisitDurationMinutes { get; set; }
    public List<DoctorServiceDto> Services { get; set; } = new();
    public DoctorVisitFieldConfigDto? VisitFieldConfig { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDoctorRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be 6-100 characters")]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Specialty { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }

    public UrgentCaseMode UrgentCaseMode { get; set; } = UrgentCaseMode.UrgentNext;

    [Range(1, 120, ErrorMessage = "AvgVisitDurationMinutes must be between 1 and 120")]
    public int AvgVisitDurationMinutes { get; set; } = 15;
}

public class UpdateDoctorRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Specialty { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }

    public UrgentCaseMode UrgentCaseMode { get; set; } = UrgentCaseMode.UrgentNext;

    [Range(1, 120)]
    public int AvgVisitDurationMinutes { get; set; } = 15;
}

public class DoctorServiceDto
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class DoctorServiceRequest
{
    [Required(ErrorMessage = "ServiceName is required")]
    [StringLength(200, ErrorMessage = "ServiceName cannot exceed 200 characters")]
    public string ServiceName { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; set; }

    [Range(1, 480)]
    public int? DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateDoctorServicesRequest
{
    [Required(ErrorMessage = "Services list is required")]
    public List<DoctorServiceRequest> Services { get; set; } = new();
}

public class DoctorVisitFieldConfigDto
{
    public bool BloodPressure { get; set; }
    public bool HeartRate { get; set; }
    public bool Temperature { get; set; }
    public bool Weight { get; set; }
    public bool Height { get; set; }
    public bool BMI { get; set; }
    public bool BloodSugar { get; set; }
    public bool OxygenSaturation { get; set; }
    public bool RespiratoryRate { get; set; }
}

public class UpdateVisitFieldsRequest
{
    public bool BloodPressure { get; set; }
    public bool HeartRate { get; set; }
    public bool Temperature { get; set; } = true;
    public bool Weight { get; set; } = true;
    public bool Height { get; set; }
    public bool BMI { get; set; }
    public bool BloodSugar { get; set; }
    public bool OxygenSaturation { get; set; }
    public bool RespiratoryRate { get; set; }
}
