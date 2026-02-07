namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Public SEO DTOs ───────────────────────────────────────────────

public class PublicClinicDto
{
    public string ClinicName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? SupportWhatsAppNumber { get; set; }
    public string? SupportPhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? LogoUrl { get; set; }
    public bool BookingEnabled { get; set; }
    public string TenantSlug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PublicDoctorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsEnabled { get; set; }
    public int AvgVisitDurationMinutes { get; set; }
    public List<PublicDoctorServiceDto> Services { get; set; } = new();
}

public class PublicDoctorServiceDto
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? DurationMinutes { get; set; }
}

public class PublicWorkingHourDto
{
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
