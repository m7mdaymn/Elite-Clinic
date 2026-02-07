namespace EliteClinic.Domain.Entities;

/// <summary>
/// An expense record for the clinic (supplies, maintenance, utilities, etc.).
/// Category is free text.
/// </summary>
public class Expense : TenantBaseEntity
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid RecordedByUserId { get; set; }

    public ApplicationUser RecordedBy { get; set; } = null!;

    public Expense()
    {
        ExpenseDate = DateTime.UtcNow.Date;
    }
}
