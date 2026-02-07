namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Finance Report DTOs ───────────────────────────────────────────

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }
}

public class DoctorRevenueDto
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public int VisitCount { get; set; }
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int InvoiceCount { get; set; }
}

public class YearlyRevenueDto
{
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int InvoiceCount { get; set; }
    public List<MonthlyRevenueDto> Months { get; set; } = new();
}

public class ProfitReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int InvoiceCount { get; set; }
    public int ExpenseCount { get; set; }
    public List<DoctorRevenueDto> ByDoctor { get; set; } = new();
}
