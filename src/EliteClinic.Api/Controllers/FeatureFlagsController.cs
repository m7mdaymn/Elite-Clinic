using EliteClinic.Application.Features.Platform.FeatureFlags.DTOs;
using EliteClinic.Application.Features.Platform.FeatureFlags.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/platform/feature-flags")]
[Authorize(Roles = "SuperAdmin")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;

    public FeatureFlagsController(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    [HttpGet("{tenantId}")]
    public async Task<IActionResult> GetFeatureFlags(Guid tenantId)
    {
        var result = await _featureFlagService.GetFeatureFlagsByTenantIdAsync(tenantId);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPut("{tenantId}")]
    public async Task<IActionResult> UpdateFeatureFlags(Guid tenantId, [FromBody] UpdateFeatureFlagRequest request)
    {
        var result = await _featureFlagService.UpdateFeatureFlagsAsync(tenantId, request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
