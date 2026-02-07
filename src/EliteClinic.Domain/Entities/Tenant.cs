using EliteClinic.Domain.Enums;

namespace EliteClinic.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public TenantStatus Status { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }

    // Navigation properties
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public TenantFeatureFlag? FeatureFlags { get; set; }

    public Tenant()
    {
        Status = TenantStatus.Inactive;
    }

    public Tenant(string name, string slug) : this()
    {
        Name = name;
        Slug = slug;
    }
}
