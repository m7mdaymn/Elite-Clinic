namespace EliteClinic.Domain.Entities;

public class Employee : TenantBaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "ClinicManager";
    public decimal? Salary { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Notes { get; set; }
    public bool IsEnabled { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;

    public Employee()
    {
        IsEnabled = true;
    }
}
