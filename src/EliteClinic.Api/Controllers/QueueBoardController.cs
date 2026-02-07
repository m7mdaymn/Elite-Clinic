using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/queue")]
[Authorize]
public class QueueBoardController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly ITenantContext _tenantContext;

    public QueueBoardController(IQueueService queueService, ITenantContext tenantContext)
    {
        _queueService = queueService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Reception board — all active sessions with ticket counts (today)
    /// </summary>
    [HttpGet("board")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueBoardDto>), 200)]
    public async Task<ActionResult<ApiResponse<QueueBoardDto>>> GetBoard()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueBoardDto>.Error("Tenant context not resolved"));

        var result = await _queueService.GetBoardAsync(_tenantContext.TenantId);
        return Ok(result);
    }

    /// <summary>
    /// Doctor's own queue — shows current session tickets for the logged-in doctor
    /// </summary>
    [HttpGet("my-queue")]
    [Authorize(Roles = "Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueBoardSessionDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<QueueBoardSessionDto>>> GetMyQueue()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueBoardSessionDto>.Error("Tenant context not resolved"));

        var result = await _queueService.GetMyQueueAsync(_tenantContext.TenantId, GetCurrentUserId());
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Patient's active ticket status
    /// </summary>
    [HttpGet("my-ticket")]
    [Authorize(Roles = "Patient,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> GetMyTicket()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.GetMyTicketAsync(_tenantContext.TenantId, GetCurrentUserId());
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
