namespace EliteClinic.Domain.Entities;

public class ClinicSettings : TenantBaseEntity
{
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

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<WorkingHour> WorkingHours { get; set; } = new List<WorkingHour>();

    public ClinicSettings()
    {
        BookingEnabled = false;
        CancellationWindowHours = 2;
    }

    public ClinicSettings(Guid tenantId, string clinicName) : this()
    {
        TenantId = tenantId;
        ClinicName = clinicName;
    }
}
