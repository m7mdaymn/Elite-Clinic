namespace EliteClinic.Domain.Entities;

public class TenantFeatureFlag : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // Feature flags with defaults matching PLAN.md §13
    public bool OnlineBooking { get; set; } = false;
    public bool WhatsappAutomation { get; set; } = true;
    public bool PwaNotifications { get; set; } = false;
    public bool ExpensesModule { get; set; } = true;
    public bool AdvancedMedicalTemplates { get; set; } = false;
    public bool Ratings { get; set; } = false;
    public bool Export { get; set; } = false;
}
