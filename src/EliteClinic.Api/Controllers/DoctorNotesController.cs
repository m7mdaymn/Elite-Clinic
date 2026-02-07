using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/doctor-notes")]
[Authorize]
public class DoctorNotesController : ControllerBase
{
    private readonly IDoctorNoteService _doctorNoteService;
    private readonly ITenantContext _tenantContext;

    public DoctorNotesController(IDoctorNoteService doctorNoteService, ITenantContext tenantContext)
    {
        _doctorNoteService = doctorNoteService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Send a note to reception (Doctor only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorNoteDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<DoctorNoteDto>>> Create([FromBody] CreateDoctorNoteRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorNoteDto>.Error("Tenant context not resolved"));

        var result = await _doctorNoteService.CreateAsync(_tenantContext.TenantId, GetCurrentUserId(), request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Get unread notes (Reception/Staff)
    /// </summary>
    [HttpGet("unread")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorNoteDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<DoctorNoteDto>>>> GetUnread()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<DoctorNoteDto>>.Error("Tenant context not resolved"));

        var result = await _doctorNoteService.GetUnreadAsync(_tenantContext.TenantId);
        return Ok(result);
    }

    /// <summary>
    /// List all notes (paginated, filterable)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorNoteDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorNoteDto>>>> GetAll(
        [FromQuery] bool? unreadOnly, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<DoctorNoteDto>>.Error("Tenant context not resolved"));

        var result = await _doctorNoteService.GetAllAsync(_tenantContext.TenantId, unreadOnly, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Mark a note as read (Reception/Staff)
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DoctorNoteDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<DoctorNoteDto>>> MarkAsRead(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<DoctorNoteDto>.Error("Tenant context not resolved"));

        var result = await _doctorNoteService.MarkAsReadAsync(_tenantContext.TenantId, id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
