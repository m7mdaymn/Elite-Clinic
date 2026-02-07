namespace EliteClinic.Domain.Entities;

/// <summary>
/// Base entity for all domain entities
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }
}

/// <summary>
/// Base entity for tenant-scoped entities
/// </summary>
public abstract class TenantBaseEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Interface for auditable entities
/// </summary>
public interface IAuditableEntity
{
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}
