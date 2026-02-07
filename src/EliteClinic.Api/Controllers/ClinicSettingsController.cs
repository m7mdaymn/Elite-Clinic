using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/settings")]
[Authorize]
public class ClinicSettingsController : ControllerBase
{
    private readonly IClinicSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ClinicSettingsController> _logger;

    public ClinicSettingsController(
        IClinicSettingsService settingsService,
        ITenantContext tenantContext,
        ILogger<ClinicSettingsController> logger)
    {
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get clinic settings for current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ClinicSettingsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<ClinicSettingsDto>>> GetSettings()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ClinicSettingsDto>.Error("Tenant context not resolved"));

        var result = await _settingsService.GetSettingsAsync(_tenantContext.TenantId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update clinic settings (ClinicOwner only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ClinicSettingsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<ClinicSettingsDto>>> UpdateSettings([FromBody] UpdateClinicSettingsRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ClinicSettingsDto>.Error("Tenant context not resolved"));

        var result = await _settingsService.UpdateSettingsAsync(_tenantContext.TenantId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }
}
