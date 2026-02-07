namespace EliteClinic.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; } // Create, Update, Delete
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }

    public AuditLog()
    {
        Timestamp = DateTime.UtcNow;
    }

    public AuditLog(Guid? userId, Guid? tenantId, string entityType, string entityId, string action)
        : this()
    {
        UserId = userId;
        TenantId = tenantId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
    }
}
