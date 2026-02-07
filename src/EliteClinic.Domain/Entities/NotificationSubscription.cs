namespace EliteClinic.Domain.Entities;

/// <summary>
/// Stores PWA push notification subscription details for a patient/user.
/// Used to deliver medication reminders, follow-up reminders, queue approaching alerts.
/// Gated by PwaNotifications feature flag.
/// </summary>
public class NotificationSubscription : TenantBaseEntity
{
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;

    public NotificationSubscription()
    {
        IsActive = true;
    }
}
