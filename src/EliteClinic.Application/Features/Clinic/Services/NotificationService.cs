using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EliteClinic.Application.Features.Clinic.Services;

public class NotificationService : INotificationService
{
    private readonly EliteClinicDbContext _context;

    public NotificationService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<NotificationSubscriptionDto>> SubscribeAsync(Guid tenantId, Guid userId, CreateNotificationSubscriptionRequest request)
    {
        // Check feature flag
        var featureFlags = await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && !f.IsDeleted);
        if (featureFlags == null || !featureFlags.PwaNotifications)
            return ApiResponse<NotificationSubscriptionDto>.Error("PWA notifications are not enabled for this clinic");

        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return ApiResponse<NotificationSubscriptionDto>.Error("Endpoint is required");

        // Check for duplicate subscription
        var existing = await _context.NotificationSubscriptions
            .FirstOrDefaultAsync(ns => ns.TenantId == tenantId && ns.UserId == userId && ns.Endpoint == request.Endpoint && !ns.IsDeleted);
        if (existing != null)
        {
            existing.IsActive = true;
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            await _context.SaveChangesAsync();
            return ApiResponse<NotificationSubscriptionDto>.Ok(MapToDto(existing), "Subscription reactivated");
        }

        var subscription = new NotificationSubscription
        {
            TenantId = tenantId,
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
            IsActive = true
        };

        _context.NotificationSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return ApiResponse<NotificationSubscriptionDto>.Created(MapToDto(subscription), "Push notification subscription created");
    }

    public async Task<ApiResponse> UnsubscribeAsync(Guid tenantId, Guid subscriptionId, Guid userId)
    {
        var subscription = await _context.NotificationSubscriptions
            .FirstOrDefaultAsync(ns => ns.Id == subscriptionId && ns.TenantId == tenantId && ns.UserId == userId && !ns.IsDeleted);
        if (subscription == null)
            return ApiResponse.Error("Subscription not found");

        subscription.IsActive = false;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Unsubscribed successfully");
    }

    public async Task<ApiResponse<List<NotificationSubscriptionDto>>> GetMySubscriptionsAsync(Guid tenantId, Guid userId)
    {
        var subscriptions = await _context.NotificationSubscriptions
            .Where(ns => ns.TenantId == tenantId && ns.UserId == userId && !ns.IsDeleted && ns.IsActive)
            .OrderByDescending(ns => ns.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<NotificationSubscriptionDto>>.Ok(
            subscriptions.Select(MapToDto).ToList(),
            $"Retrieved {subscriptions.Count} subscription(s)");
    }

    public async Task<ApiResponse<MessageLogDto>> SendNotificationAsync(Guid tenantId, SendNotificationRequest request)
    {
        // Check feature flag
        var featureFlags = await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && !f.IsDeleted);
        if (featureFlags == null || !featureFlags.PwaNotifications)
            return ApiResponse<MessageLogDto>.Error("PWA notifications are not enabled for this clinic");

        // Check user has active subscriptions
        var hasSubscription = await _context.NotificationSubscriptions
            .AnyAsync(ns => ns.TenantId == tenantId && ns.UserId == request.UserId && ns.IsActive && !ns.IsDeleted);
        if (!hasSubscription)
            return ApiResponse<MessageLogDto>.Error("User has no active push notification subscriptions");

        // Build variables
        var variables = request.Variables ?? new Dictionary<string, string>();
        variables["title"] = request.Title;
        variables["body"] = request.Body;

        // Log as MessageLog (PWA channel)
        var messageLog = new MessageLog
        {
            TenantId = tenantId,
            TemplateName = request.TemplateName ?? "custom_notification",
            RecipientUserId = request.UserId,
            Channel = MessageChannel.PWA,
            Status = MessageStatus.Sent,
            AttemptCount = 1,
            LastAttemptAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            Variables = JsonSerializer.Serialize(variables)
        };

        _context.MessageLogs.Add(messageLog);

        // Update LastUsedAt on subscriptions
        var subs = await _context.NotificationSubscriptions
            .Where(ns => ns.TenantId == tenantId && ns.UserId == request.UserId && ns.IsActive && !ns.IsDeleted)
            .ToListAsync();
        foreach (var sub in subs)
            sub.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<MessageLogDto>.Created(new MessageLogDto
        {
            Id = messageLog.Id,
            TemplateName = messageLog.TemplateName,
            RecipientUserId = messageLog.RecipientUserId,
            Channel = messageLog.Channel,
            Status = messageLog.Status,
            AttemptCount = messageLog.AttemptCount,
            LastAttemptAt = messageLog.LastAttemptAt,
            SentAt = messageLog.SentAt,
            Variables = messageLog.Variables,
            CreatedAt = messageLog.CreatedAt
        }, "PWA notification sent successfully");
    }

    private static NotificationSubscriptionDto MapToDto(NotificationSubscription ns) => new()
    {
        Id = ns.Id,
        UserId = ns.UserId,
        Endpoint = ns.Endpoint,
        IsActive = ns.IsActive,
        LastUsedAt = ns.LastUsedAt,
        CreatedAt = ns.CreatedAt
    };
}
