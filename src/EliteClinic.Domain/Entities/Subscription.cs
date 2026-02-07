using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";

    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public string? Notes { get; set; }

    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
