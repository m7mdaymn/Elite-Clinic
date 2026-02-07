using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EliteClinic.Application.Features.Clinic.Services;

public class MessageService : IMessageService
{
    private readonly EliteClinicDbContext _context;

    // Valid template names from MESSAGE_SPEC.md
    private static readonly HashSet<string> ValidTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        "patient_credentials", "booking_confirmation", "queue_ticket_issued",
        "your_turn", "visit_summary", "followup_reminder", "password_reset",
        "medication_reminder", "followup_reminder_pwa", "queue_approaching_pwa"
    };

    public MessageService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<MessageLogDto>> SendMessageAsync(Guid tenantId, SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName))
            return ApiResponse<MessageLogDto>.Error("Template name is required");

        if (!ValidTemplates.Contains(request.TemplateName))
            return ApiResponse<MessageLogDto>.Error($"Invalid template name: {request.TemplateName}. Valid templates: {string.Join(", ", ValidTemplates)}");

        if (request.Channel == MessageChannel.WhatsApp && string.IsNullOrWhiteSpace(request.RecipientPhone))
            return ApiResponse<MessageLogDto>.Error("Recipient phone is required for WhatsApp messages");

        if (request.Channel == MessageChannel.PWA && !request.RecipientUserId.HasValue)
            return ApiResponse<MessageLogDto>.Error("Recipient user ID is required for PWA notifications");

        var messageLog = new MessageLog
        {
            TenantId = tenantId,
            TemplateName = request.TemplateName,
            RecipientPhone = request.RecipientPhone,
            RecipientUserId = request.RecipientUserId,
            Channel = request.Channel,
            Status = MessageStatus.Pending,
            AttemptCount = 0,
            Variables = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null
        };

        _context.MessageLogs.Add(messageLog);
        await _context.SaveChangesAsync();

        // Simulate sending: move to Sent status (infrastructure-only — no actual WhatsApp API call)
        messageLog.Status = MessageStatus.Sent;
        messageLog.AttemptCount = 1;
        messageLog.LastAttemptAt = DateTime.UtcNow;
        messageLog.SentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<MessageLogDto>.Created(MapToDto(messageLog), "Message queued and sent successfully");
    }

    public async Task<ApiResponse<MessageLogDto>> RetryMessageAsync(Guid tenantId, Guid messageId)
    {
        var message = await _context.MessageLogs
            .FirstOrDefaultAsync(m => m.Id == messageId && m.TenantId == tenantId && !m.IsDeleted);

        if (message == null)
            return ApiResponse<MessageLogDto>.Error("Message not found");

        if (message.Status != MessageStatus.Failed)
            return ApiResponse<MessageLogDto>.Error("Only failed messages can be retried");

        if (message.AttemptCount >= 3)
            return ApiResponse<MessageLogDto>.Error("Maximum retry attempts (3) reached");

        // Simulate retry
        message.Status = MessageStatus.Sent;
        message.AttemptCount++;
        message.LastAttemptAt = DateTime.UtcNow;
        message.SentAt = DateTime.UtcNow;
        message.FailureReason = null;
        await _context.SaveChangesAsync();

        return ApiResponse<MessageLogDto>.Ok(MapToDto(message), "Message retried successfully");
    }

    public async Task<ApiResponse<PagedResult<MessageLogDto>>> GetAllAsync(Guid tenantId, string? templateName,
        string? channel, string? status, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.MessageLogs
            .Where(m => m.TenantId == tenantId && !m.IsDeleted);

        if (!string.IsNullOrEmpty(templateName))
            query = query.Where(m => m.TemplateName == templateName);

        if (!string.IsNullOrEmpty(channel) && Enum.TryParse<MessageChannel>(channel, true, out var channelEnum))
            query = query.Where(m => m.Channel == channelEnum);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MessageStatus>(status, true, out var statusEnum))
            query = query.Where(m => m.Status == statusEnum);

        var totalCount = await query.CountAsync();
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<MessageLogDto>
        {
            Items = messages.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<MessageLogDto>>.Ok(result, $"Retrieved {result.Items.Count} message(s)");
    }

    public async Task<ApiResponse<MessageLogDto>> GetByIdAsync(Guid tenantId, Guid messageId)
    {
        var message = await _context.MessageLogs
            .FirstOrDefaultAsync(m => m.Id == messageId && m.TenantId == tenantId && !m.IsDeleted);

        if (message == null)
            return ApiResponse<MessageLogDto>.Error("Message not found");

        return ApiResponse<MessageLogDto>.Ok(MapToDto(message));
    }

    private static MessageLogDto MapToDto(MessageLog m) => new()
    {
        Id = m.Id,
        TemplateName = m.TemplateName,
        RecipientPhone = m.RecipientPhone,
        RecipientUserId = m.RecipientUserId,
        Channel = m.Channel,
        Status = m.Status,
        AttemptCount = m.AttemptCount,
        LastAttemptAt = m.LastAttemptAt,
        SentAt = m.SentAt,
        DeliveredAt = m.DeliveredAt,
        FailureReason = m.FailureReason,
        Variables = m.Variables,
        CreatedAt = m.CreatedAt
    };
}
