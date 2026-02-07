namespace EliteClinic.Domain.Entities;

/// <summary>
/// Doctor-to-reception instant note for internal communication.
/// One-way: doctor sends, reception reads and acknowledges.
/// </summary>
public class DoctorNote : TenantBaseEntity
{
    public Guid DoctorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? ReadByUserId { get; set; }

    // Navigation
    public Doctor Doctor { get; set; } = null!;

    public DoctorNote()
    {
        IsRead = false;
    }
}
