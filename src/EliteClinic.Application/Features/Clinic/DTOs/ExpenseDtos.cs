namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Expense DTOs ──────────────────────────────────────────────────

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid RecordedByUserId { get; set; }
    public string RecordedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateExpenseRequest
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpenseDate { get; set; }
}

public class UpdateExpenseRequest
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpenseDate { get; set; }
}
