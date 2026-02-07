using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/queue/sessions")]
[Authorize]
public class QueueSessionsController : ControllerBase
{
    private readonly IQueueService _queueService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<QueueSessionsController> _logger;

    public QueueSessionsController(IQueueService queueService, ITenantContext tenantContext, ILogger<QueueSessionsController> logger)
    {
        _queueService = queueService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Open a new queue session (ClinicOwner, ClinicManager, or Doctor for own session)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueSessionDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueSessionDto>>> OpenSession([FromBody] CreateQueueSessionRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueSessionDto>.Error("Tenant context not resolved"));

        var result = await _queueService.OpenSessionAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        _logger.LogInformation("Queue session opened for tenant {TenantId}", _tenantContext.TenantId);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Close a queue session — remaining Waiting/Called tickets become NoShow
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueSessionDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QueueSessionDto>>> CloseSession(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueSessionDto>.Error("Tenant context not resolved"));

        var result = await _queueService.CloseSessionAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// List all queue sessions (paginated)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QueueSessionDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<QueueSessionDto>>>> GetSessions(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<QueueSessionDto>>.Error("Tenant context not resolved"));

        var result = await _queueService.GetSessionsAsync(_tenantContext.TenantId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get session by ID with ticket summary
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QueueSessionDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<QueueSessionDto>>> GetSession(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<QueueSessionDto>.Error("Tenant context not resolved"));

        var result = await _queueService.GetSessionByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all tickets for a session (ordered: urgent first, then by issued time)
    /// </summary>
    [HttpGet("{id:guid}/tickets")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<QueueTicketDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<List<QueueTicketDto>>>> GetTickets(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<QueueTicketDto>>.Error("Tenant context not resolved"));

        var result = await _queueService.GetTicketsBySessionAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
