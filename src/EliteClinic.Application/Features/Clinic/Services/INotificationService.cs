using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface INotificationService
{
    Task<ApiResponse<NotificationSubscriptionDto>> SubscribeAsync(Guid tenantId, Guid userId, CreateNotificationSubscriptionRequest request);
    Task<ApiResponse> UnsubscribeAsync(Guid tenantId, Guid subscriptionId, Guid userId);
    Task<ApiResponse<List<NotificationSubscriptionDto>>> GetMySubscriptionsAsync(Guid tenantId, Guid userId);
    Task<ApiResponse<MessageLogDto>> SendNotificationAsync(Guid tenantId, SendNotificationRequest request);
}
