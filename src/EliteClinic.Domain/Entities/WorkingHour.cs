namespace EliteClinic.Domain.Entities;

public class WorkingHour : TenantBaseEntity
{
    public Guid ClinicSettingsId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public ClinicSettings ClinicSettings { get; set; } = null!;

    public WorkingHour()
    {
        IsActive = true;
    }
}
