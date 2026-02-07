namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Notification Subscription DTOs ────────────────────────────────

public class NotificationSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateName { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}
