namespace EliteClinic.Domain.Entities;

public class DoctorVisitFieldConfig : TenantBaseEntity
{
    public Guid DoctorId { get; set; }
    public bool BloodPressure { get; set; }
    public bool HeartRate { get; set; }
    public bool Temperature { get; set; }
    public bool Weight { get; set; }
    public bool Height { get; set; }
    public bool BMI { get; set; }
    public bool BloodSugar { get; set; }
    public bool OxygenSaturation { get; set; }
    public bool RespiratoryRate { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;

    public DoctorVisitFieldConfig()
    {
        Temperature = true;
        Weight = true;
    }
}
