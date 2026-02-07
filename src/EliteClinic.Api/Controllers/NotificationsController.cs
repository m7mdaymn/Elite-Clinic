using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ITenantContext _tenantContext;

    public NotificationsController(INotificationService notificationService, ITenantContext tenantContext)
    {
        _notificationService = notificationService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Subscribe to push notifications
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize(Roles = "Patient,Doctor,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<NotificationSubscriptionDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<NotificationSubscriptionDto>>> Subscribe([FromBody] CreateNotificationSubscriptionRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<NotificationSubscriptionDto>.Error("Tenant context not resolved"));

        var result = await _notificationService.SubscribeAsync(_tenantContext.TenantId, GetCurrentUserId(), request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Unsubscribe from push notifications
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Patient,Doctor,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse>> Unsubscribe(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse.Error("Tenant context not resolved"));

        var result = await _notificationService.UnsubscribeAsync(_tenantContext.TenantId, id, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get my notification subscriptions
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient,Doctor,ClinicOwner,ClinicManager,Receptionist,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationSubscriptionDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<NotificationSubscriptionDto>>>> GetMySubscriptions()
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<NotificationSubscriptionDto>>.Error("Tenant context not resolved"));

        var result = await _notificationService.GetMySubscriptionsAsync(_tenantContext.TenantId, GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>
    /// Send a push notification to a user
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,Receptionist,Doctor,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MessageLogDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<MessageLogDto>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<MessageLogDto>.Error("Tenant context not resolved"));

        var result = await _notificationService.SendNotificationAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }
}
