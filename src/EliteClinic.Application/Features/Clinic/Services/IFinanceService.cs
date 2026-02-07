using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IFinanceService
{
    Task<ApiResponse<DailyRevenueDto>> GetDailyRevenueAsync(Guid tenantId, DateTime date);
    Task<ApiResponse<List<DoctorRevenueDto>>> GetRevenueByDoctorAsync(Guid tenantId, DateTime date, Guid? doctorId);
    Task<ApiResponse<MonthlyRevenueDto>> GetMonthlyRevenueAsync(Guid tenantId, int year, int month);
    Task<ApiResponse<YearlyRevenueDto>> GetYearlyRevenueAsync(Guid tenantId, int year);
    Task<ApiResponse<ProfitReportDto>> GetProfitReportAsync(Guid tenantId, DateTime from, DateTime to);
}
