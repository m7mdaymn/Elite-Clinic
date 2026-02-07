using EliteClinic.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EliteClinic.Application.Features.Platform.Subscriptions.DTOs;

public class CreateSubscriptionRequest
{
    [Required(ErrorMessage = "TenantId is required")]
    public Guid TenantId { get; set; }

    [Required(ErrorMessage = "PlanName is required")]
    [StringLength(100, ErrorMessage = "PlanName cannot exceed 100 characters")]
    public string PlanName { get; set; } = string.Empty;

    [Required(ErrorMessage = "StartDate is required")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "EndDate is required")]
    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
    public string Currency { get; set; } = "EGP";

    public string? Notes { get; set; }
}

public class ExtendSubscriptionRequest
{
    [Required(ErrorMessage = "NewEndDate is required")]
    public DateTime NewEndDate { get; set; }

    public string? Notes { get; set; }
}

public class CancelSubscriptionRequest
{
    [Required(ErrorMessage = "CancelReason is required")]
    [StringLength(500, ErrorMessage = "CancelReason cannot exceed 500 characters")]
    public string CancelReason { get; set; } = string.Empty;
}

public class MarkPaidRequest
{
    [Required(ErrorMessage = "PaymentMethod is required")]
    [StringLength(100, ErrorMessage = "PaymentMethod cannot exceed 100 characters")]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "PaymentReference cannot exceed 200 characters")]
    public string? PaymentReference { get; set; }

    public DateTime? PaidAt { get; set; }
}

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
