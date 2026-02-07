using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ITenantContext _tenantContext;

    public MessagesController(IMessageService messageService, ITenantContext tenantContext)
    {
        _messageService = messageService;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Send a message (WhatsApp or PWA) using a template
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MessageLogDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<MessageLogDto>>> Send([FromBody] SendMessageRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<MessageLogDto>.Error("Tenant context not resolved"));

        var result = await _messageService.SendMessageAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Retry a failed message
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MessageLogDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<MessageLogDto>>> Retry(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<MessageLogDto>.Error("Tenant context not resolved"));

        var result = await _messageService.RetryMessageAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get message by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MessageLogDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<MessageLogDto>>> GetById(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<MessageLogDto>.Error("Tenant context not resolved"));

        var result = await _messageService.GetByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// List all messages (paginated, filterable by template, channel, status)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MessageLogDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageLogDto>>>> GetAll(
        [FromQuery] string? templateName, [FromQuery] string? channel, [FromQuery] string? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<MessageLogDto>>.Error("Tenant context not resolved"));

        var result = await _messageService.GetAllAsync(_tenantContext.TenantId, templateName, channel, status, pageNumber, pageSize);
        return Ok(result);
    }
}
