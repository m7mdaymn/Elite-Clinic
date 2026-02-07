namespace EliteClinic.Domain.Entities;

public class DoctorService : TenantBaseEntity
{
    public Guid DoctorId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;

    public DoctorService()
    {
        IsActive = true;
    }
}
