using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ITenantContext _tenantContext;

    public BookingsController(IBookingService bookingService, ITenantContext tenantContext)
    {
        _bookingService = bookingService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Create a booking (Patient role — existing patients only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Patient,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create([FromBody] CreateBookingRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<BookingDto>.Error("Tenant context not resolved"));

        var result = await _bookingService.CreateAsync(_tenantContext.TenantId, GetCurrentUserId(), request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Patient,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(Guid id, [FromBody] CancelBookingRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<BookingDto>.Error("Tenant context not resolved"));

        var result = await _bookingService.CancelAsync(_tenantContext.TenantId, id, GetCurrentUserId(), request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Reschedule a booking
    /// </summary>
    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Roles = "Patient,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Reschedule(Guid id, [FromBody] RescheduleBookingRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<BookingDto>.Error("Tenant context not resolved"));

        var result = await _bookingService.RescheduleAsync(_tenantContext.TenantId, id, GetCurrentUserId(), request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get booking by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Patient,ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<BookingDto>.Error("Tenant context not resolved"));

        var result = await _bookingService.GetByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// List all bookings (paginated, filterable)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BookingDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookingDto>>>> GetAll(
        [FromQuery] Guid? patientId, [FromQuery] Guid? doctorId, [FromQuery] string? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<BookingDto>>.Error("Tenant context not resolved"));

        var result = await _bookingService.GetAllAsync(_tenantContext.TenantId, patientId, doctorId, status, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get my bookings (Patient)
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(ApiResponse<List<BookingDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<BookingDto>>>> GetMyBookings()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<BookingDto>>.Error("Tenant context not resolved"));

        var result = await _bookingService.GetMyBookingsAsync(_tenantContext.TenantId, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
