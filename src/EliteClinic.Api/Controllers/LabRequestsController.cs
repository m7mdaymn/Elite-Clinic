using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/visits/{visitId:guid}/labs")]
[Authorize]
public class LabRequestsController : ControllerBase
{
    private readonly ILabRequestService _labRequestService;
    private readonly ITenantContext _tenantContext;

    public LabRequestsController(ILabRequestService labRequestService, ITenantContext tenantContext)
    {
        _labRequestService = labRequestService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Add lab/imaging request to visit (Doctor only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LabRequestDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LabRequestDto>>> Create(Guid visitId, [FromBody] CreateLabRequestRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<LabRequestDto>.Error("Tenant context not resolved"));

        var result = await _labRequestService.CreateAsync(_tenantContext.TenantId, visitId, request, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Update lab/imaging request (Doctor, same-day only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LabRequestDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LabRequestDto>>> Update(Guid visitId, Guid id, [FromBody] UpdateLabRequestRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<LabRequestDto>.Error("Tenant context not resolved"));

        var result = await _labRequestService.UpdateAsync(_tenantContext.TenantId, visitId, id, request, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Add result to a lab/imaging request (ClinicOwner, ClinicManager)
    /// </summary>
    [HttpPost("{id:guid}/result")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LabRequestDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LabRequestDto>>> AddResult(Guid visitId, Guid id, [FromBody] AddLabResultRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<LabRequestDto>.Error("Tenant context not resolved"));

        var result = await _labRequestService.AddResultAsync(_tenantContext.TenantId, visitId, id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// List all lab/imaging requests for a visit
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<LabRequestDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<LabRequestDto>>>> GetByVisit(Guid visitId)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<LabRequestDto>>.Error("Tenant context not resolved"));

        var result = await _labRequestService.GetByVisitAsync(_tenantContext.TenantId, visitId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
