using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.Tenants.DTOs;
using EliteClinic.Application.Features.Platform.Tenants.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var result = await _tenantService.CreateTenantAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetTenantById), new { id = result.Data!.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTenants(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _tenantService.GetAllTenantsAsync(pageNumber, pageSize, searchTerm);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenantById(Guid id)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var result = await _tenantService.UpdateTenantAsync(id, request);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id)
    {
        var result = await _tenantService.ActivateTenantAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        var result = await _tenantService.SuspendTenantAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> BlockTenant(Guid id)
    {
        var result = await _tenantService.BlockTenantAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var result = await _tenantService.DeleteTenantAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
