using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;
    private readonly ITenantContext _tenantContext;

    public FinanceController(IFinanceService financeService, ITenantContext tenantContext)
    {
        _financeService = financeService;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Daily revenue summary
    /// </summary>
    [HttpGet("daily")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DailyRevenueDto>), 200)]
    public async Task<ActionResult<ApiResponse<DailyRevenueDto>>> GetDailyRevenue([FromQuery] DateTime? date)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DailyRevenueDto>.Error("Tenant context not resolved"));

        var targetDate = date ?? DateTime.UtcNow;
        var result = await _financeService.GetDailyRevenueAsync(_tenantContext.TenantId, targetDate);
        return Ok(result);
    }

    /// <summary>
    /// Revenue breakdown by doctor
    /// </summary>
    [HttpGet("by-doctor")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorRevenueDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<DoctorRevenueDto>>>> GetByDoctor(
        [FromQuery] DateTime? date, [FromQuery] Guid? doctorId)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<DoctorRevenueDto>>.Error("Tenant context not resolved"));

        var targetDate = date ?? DateTime.UtcNow;
        var result = await _financeService.GetRevenueByDoctorAsync(_tenantContext.TenantId, targetDate, doctorId);
        return Ok(result);
    }

    /// <summary>
    /// Monthly revenue summary with expenses and net profit
    /// </summary>
    [HttpGet("monthly")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyRevenueDto>), 200)]
    public async Task<ActionResult<ApiResponse<MonthlyRevenueDto>>> GetMonthly(
        [FromQuery] int? year, [FromQuery] int? month)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<MonthlyRevenueDto>.Error("Tenant context not resolved"));

        var now = DateTime.UtcNow;
        var result = await _financeService.GetMonthlyRevenueAsync(_tenantContext.TenantId, year ?? now.Year, month ?? now.Month);
        return Ok(result);
    }

    /// <summary>
    /// Yearly revenue summary with monthly breakdown
    /// </summary>
    [HttpGet("yearly")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<YearlyRevenueDto>), 200)]
    public async Task<ActionResult<ApiResponse<YearlyRevenueDto>>> GetYearly([FromQuery] int? year)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<YearlyRevenueDto>.Error("Tenant context not resolved"));

        var result = await _financeService.GetYearlyRevenueAsync(_tenantContext.TenantId, year ?? DateTime.UtcNow.Year);
        return Ok(result);
    }

    /// <summary>
    /// Profit report (revenue minus expenses) for a date range
    /// </summary>
    [HttpGet("profit")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProfitReportDto>), 200)]
    public async Task<ActionResult<ApiResponse<ProfitReportDto>>> GetProfit(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ProfitReportDto>.Error("Tenant context not resolved"));

        var fromDate = from ?? DateTime.UtcNow.Date;
        var toDate = to ?? DateTime.UtcNow.Date;
        var result = await _financeService.GetProfitReportAsync(_tenantContext.TenantId, fromDate, toDate);
        return Ok(result);
    }
}
