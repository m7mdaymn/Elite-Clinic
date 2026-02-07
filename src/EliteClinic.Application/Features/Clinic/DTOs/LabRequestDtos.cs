using EliteClinic.Domain.Enums;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Lab Request DTOs ──────────────────────────────────────────────

public class LabRequestDto
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public LabRequestType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }
    public string? ResultText { get; set; }
    public DateTime? ResultReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLabRequestRequest
{
    public string TestName { get; set; } = string.Empty;
    public LabRequestType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }
}

public class UpdateLabRequestRequest
{
    public string TestName { get; set; } = string.Empty;
    public LabRequestType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }
}

public class AddLabResultRequest
{
    public string ResultText { get; set; } = string.Empty;
}
