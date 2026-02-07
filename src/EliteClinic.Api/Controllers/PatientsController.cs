using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        ITenantContext tenantContext,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new patient (ClinicOwner, ClinicManager)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CreatePatientResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<CreatePatientResponse>>> CreatePatient([FromBody] CreatePatientRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<CreatePatientResponse>.Error("Tenant context not resolved"));

        var result = await _patientService.CreatePatientAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        _logger.LogInformation("Patient created: {Name} for tenant {TenantId}", request.Name, _tenantContext.TenantId);
        return StatusCode(201, result);
    }

    /// <summary>
    /// List all patients (paginated, searchable)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetAllPatients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<PatientDto>>.Error("Tenant context not resolved"));

        var result = await _patientService.GetAllPatientsAsync(_tenantContext.TenantId, pageNumber, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// Get patient by ID (with sub-profiles)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetPatientById(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PatientDto>.Error("Tenant context not resolved"));

        var result = await _patientService.GetPatientByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update patient profile
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> UpdatePatient(Guid id, [FromBody] UpdatePatientRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PatientDto>.Error("Tenant context not resolved"));

        var result = await _patientService.UpdatePatientAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Add sub-profile (child/dependent) to a patient
    /// </summary>
    [HttpPost("{id:guid}/profiles")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> AddSubProfile(Guid id, [FromBody] AddSubProfileRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PatientDto>.Error("Tenant context not resolved"));

        var result = await _patientService.AddSubProfileAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Reset patient password (staff-initiated)
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<ResetPasswordResponse>>> ResetPassword(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ResetPasswordResponse>.Error("Tenant context not resolved"));

        var result = await _patientService.ResetPasswordAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a patient (ClinicOwner only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePatient(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<object>.Error("Tenant context not resolved"));

        var result = await _patientService.DeletePatientAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
