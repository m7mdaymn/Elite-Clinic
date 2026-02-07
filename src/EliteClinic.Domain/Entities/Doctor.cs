using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

public class Doctor : TenantBaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsEnabled { get; set; }
    public UrgentCaseMode UrgentCaseMode { get; set; }
    public int AvgVisitDurationMinutes { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<DoctorService> Services { get; set; } = new List<DoctorService>();
    public DoctorVisitFieldConfig? VisitFieldConfig { get; set; }

    public Doctor()
    {
        IsEnabled = true;
        UrgentCaseMode = UrgentCaseMode.UrgentNext;
        AvgVisitDurationMinutes = 15;
    }
}
