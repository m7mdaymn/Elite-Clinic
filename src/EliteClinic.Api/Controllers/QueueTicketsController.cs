using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/queue/tickets")]
[Authorize]
public class QueueTicketsController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<QueueTicketsController> _logger;

    public QueueTicketsController(IQueueService queueService, ITenantContext tenantContext, ILogger<QueueTicketsController> logger)
    {
        _queueService = queueService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Issue a ticket to a patient for a doctor in a session (reception)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> IssueTicket([FromBody] CreateQueueTicketRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.IssueTicketAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Call next patient (Doctor calls a patient from waiting list)
    /// </summary>
    [HttpPost("{id:guid}/call")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> CallTicket(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.CallTicketAsync(_tenantContext.TenantId, id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Start visit — transitions ticket to InVisit and auto-creates a Visit entity
    /// </summary>
    [HttpPost("{id:guid}/start-visit")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> StartVisit(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.StartVisitFromTicketAsync(_tenantContext.TenantId, id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Finish ticket — marks ticket and linked visit as completed
    /// </summary>
    [HttpPost("{id:guid}/finish")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> FinishTicket(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.FinishTicketAsync(_tenantContext.TenantId, id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Skip ticket — patient didn't answer when called
    /// </summary>
    [HttpPost("{id:guid}/skip")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> SkipTicket(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.SkipTicketAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Cancel ticket — cancel before seeing doctor
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> CancelTicket(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.CancelTicketAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Mark ticket as urgent — elevates priority in the queue
    /// </summary>
    [HttpPost("{id:guid}/urgent")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueTicketDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueTicketDto>>> MarkUrgent(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueTicketDto>.Error("Tenant context not resolved"));

        var result = await _queueService.MarkUrgentAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
