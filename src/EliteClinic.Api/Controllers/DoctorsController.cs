using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(
        IDoctorService doctorService,
        ITenantContext tenantContext,
        ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new doctor (ClinicOwner only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> CreateDoctor([FromBody] CreateDoctorRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.CreateDoctorAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        _logger.LogInformation("Doctor created: {Name} for tenant {TenantId}", request.Name, _tenantContext.TenantId);
        return StatusCode(201, result);
    }

    /// <summary>
    /// List all doctors (paginated)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorDto>>>> GetAllDoctors(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<DoctorDto>>.Error("Tenant context not resolved"));

        var result = await _doctorService.GetAllDoctorsAsync(_tenantContext.TenantId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get doctor by ID (with services and visit field config)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetDoctorById(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.GetDoctorByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update doctor profile (ClinicOwner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.UpdateDoctorAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Enable doctor (ClinicOwner only)
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> EnableDoctor(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.EnableDoctorAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Disable doctor (ClinicOwner only)
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> DisableDoctor(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.DisableDoctorAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update doctor services (replace all)
    /// </summary>
    [HttpPut("{id:guid}/services")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorServiceDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<List<DoctorServiceDto>>>> UpdateServices(Guid id, [FromBody] UpdateDoctorServicesRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<DoctorServiceDto>>.Error("Tenant context not resolved"));

        var result = await _doctorService.UpdateServicesAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Update doctor visit field configuration
    /// </summary>
    [HttpPut("{id:guid}/visit-fields")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorVisitFieldConfigDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<DoctorVisitFieldConfigDto>>> UpdateVisitFields(Guid id, [FromBody] UpdateVisitFieldsRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorVisitFieldConfigDto>.Error("Tenant context not resolved"));

        var result = await _doctorService.UpdateVisitFieldsAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
