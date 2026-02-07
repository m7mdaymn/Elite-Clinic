using EliteClinic.Domain.Enums;

namespace EliteClinic.Infrastructure.Services;

public interface ITenantContext
{
    Guid TenantId { get; }
    string? TenantSlug { get; }
    TenantStatus TenantStatus { get; }
    bool IsTenantResolved { get; }
    string? UserId { get; }
}

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public TenantStatus TenantStatus { get; set; }
    public bool IsTenantResolved { get; set; }
    public string? UserId { get; set; }

    public TenantContext()
    {
        IsTenantResolved = false;
    }
}
