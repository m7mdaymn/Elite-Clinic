using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.Subscriptions.DTOs;

namespace EliteClinic.Application.Features.Platform.Subscriptions.Services;

public interface ISubscriptionService
{
    Task<ApiResponse<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<ApiResponse<PagedResult<SubscriptionDto>>> GetAllSubscriptionsAsync(int pageNumber = 1, int pageSize = 10, Guid? tenantId = null);
    Task<ApiResponse<SubscriptionDto>> ExtendSubscriptionAsync(Guid id, ExtendSubscriptionRequest request);
    Task<ApiResponse<SubscriptionDto>> CancelSubscriptionAsync(Guid id, CancelSubscriptionRequest request);
    Task<ApiResponse<SubscriptionDto>> MarkPaidAsync(Guid id, MarkPaidRequest request);
}
