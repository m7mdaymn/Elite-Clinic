namespace EliteClinic.Domain.Entities;

/// <summary>
/// A medication prescribed during a visit.
/// </summary>
public class Prescription : TenantBaseEntity
{
    public Guid VisitId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }

    public Visit Visit { get; set; } = null!;
}
