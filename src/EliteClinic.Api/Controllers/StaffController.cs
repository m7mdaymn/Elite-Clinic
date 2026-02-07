using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        IStaffService staffService,
        ITenantContext tenantContext,
        ILogger<StaffController> logger)
    {
        _staffService = staffService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new staff member (ClinicOwner only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> CreateStaff([FromBody] CreateStaffRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<StaffDto>.Error("Tenant context not resolved"));

        var result = await _staffService.CreateStaffAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        _logger.LogInformation("Staff member created: {Name} for tenant {TenantId}", request.Name, _tenantContext.TenantId);
        return StatusCode(201, result);
    }

    /// <summary>
    /// List all staff members (paginated)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StaffDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<StaffDto>>>> GetAllStaff(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<StaffDto>>.Error("Tenant context not resolved"));

        var result = await _staffService.GetAllStaffAsync(_tenantContext.TenantId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get staff member by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> GetStaffById(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<StaffDto>.Error("Tenant context not resolved"));

        var result = await _staffService.GetStaffByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update staff member (ClinicOwner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<StaffDto>.Error("Tenant context not resolved"));

        var result = await _staffService.UpdateStaffAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Enable staff member (ClinicOwner only)
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> EnableStaff(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<StaffDto>.Error("Tenant context not resolved"));

        var result = await _staffService.EnableStaffAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Disable staff member (ClinicOwner only)
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<StaffDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> DisableStaff(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<StaffDto>.Error("Tenant context not resolved"));

        var result = await _staffService.DisableStaffAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
