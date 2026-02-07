using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

/// <summary>
/// A lab test or imaging request during a visit.
/// </summary>
public class LabRequest : TenantBaseEntity
{
    public Guid VisitId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public LabRequestType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }
    public string? ResultText { get; set; }
    public DateTime? ResultReceivedAt { get; set; }

    public Visit Visit { get; set; } = null!;

    public LabRequest()
    {
        Type = LabRequestType.Lab;
        IsUrgent = false;
    }
}
