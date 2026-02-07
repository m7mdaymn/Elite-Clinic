using EliteClinic.Application.Features.Platform.Subscriptions.DTOs;
using EliteClinic.Application.Features.Platform.Subscriptions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/platform/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var result = await _subscriptionService.CreateSubscriptionAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetAllSubscriptions), new { tenantId = result.Data!.TenantId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSubscriptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? tenantId = null)
    {
        var result = await _subscriptionService.GetAllSubscriptionsAsync(pageNumber, pageSize, tenantId);
        return Ok(result);
    }

    [HttpPost("{id}/extend")]
    public async Task<IActionResult> ExtendSubscription(Guid id, [FromBody] ExtendSubscriptionRequest request)
    {
        var result = await _subscriptionService.ExtendSubscriptionAsync(id, request);
       
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid id, [FromBody] CancelSubscriptionRequest request)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(id, request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, [FromBody] MarkPaidRequest request)
    {
        var result = await _subscriptionService.MarkPaidAsync(id, request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
