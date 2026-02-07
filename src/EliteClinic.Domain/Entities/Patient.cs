using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

public class Patient : TenantBaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public Guid? ParentPatientId { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Patient? ParentPatient { get; set; }
    public ICollection<Patient> SubProfiles { get; set; } = new List<Patient>();

    public Patient()
    {
        IsDefault = true;
        Gender = Gender.Male;
    }
}
