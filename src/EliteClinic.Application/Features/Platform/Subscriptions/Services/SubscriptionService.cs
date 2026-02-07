using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.Subscriptions.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Platform.Subscriptions.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly EliteClinicDbContext _context;

    public SubscriptionService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        // Validate tenant exists
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && !t.IsDeleted);

        if (tenant == null)
        {
            return ApiResponse<SubscriptionDto>.Error("Tenant not found");
        }

        // Validate date range
        if (request.EndDate <= request.StartDate)
        {
            return ApiResponse<SubscriptionDto>.ValidationError(new List<object>
            {
                new { field = "EndDate", message = "EndDate must be after StartDate" }
            }, "EndDate must be after StartDate");
        }

        var subscription = new Subscription
        {
            TenantId = request.TenantId,
            PlanName = request.PlanName,
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value,
            Amount = request.Amount,
            Currency = request.Currency,
            IsPaid = false,
            Status = SubscriptionStatus.Active,
            Notes = request.Notes
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var dto = await MapToDtoAsync(subscription);
        return ApiResponse<SubscriptionDto>.Created(dto, "Subscription created successfully");
    }

    public async Task<ApiResponse<PagedResult<SubscriptionDto>>> GetAllSubscriptionsAsync(
        int pageNumber = 1, int pageSize = 10, Guid? tenantId = null)
    {
        var query = _context.Subscriptions
            .Include(s => s.Tenant)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == tenantId.Value);
        }

        var totalCount = await query.CountAsync();
        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<SubscriptionDto>();
        foreach (var sub in subscriptions)
        {
            dtos.Add(await MapToDtoAsync(sub));
        }

        var result = new PagedResult<SubscriptionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<SubscriptionDto>>.Ok(result, $"Retrieved {dtos.Count} subscription(s)");
    }

    public async Task<ApiResponse<SubscriptionDto>> ExtendSubscriptionAsync(Guid id, ExtendSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (subscription == null)
        {
            return ApiResponse<SubscriptionDto>.Error("Subscription not found");
        }

        // Cannot extend cancelled subscription
        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return ApiResponse<SubscriptionDto>.Error("Cannot extend a cancelled subscription");
        }

        // Validate new end date is after current end date
        if (request.NewEndDate <= subscription.EndDate)
        {
            return ApiResponse<SubscriptionDto>.ValidationError(new List<object>
            {
                new { field = "NewEndDate", message = "NewEndDate must be after the current EndDate" }
            }, "NewEndDate must be after the current EndDate");
        }

        subscription.EndDate = request.NewEndDate;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            subscription.Notes = request.Notes;
        }

        // Reactivate if expired
        if (subscription.Status == SubscriptionStatus.Expired)
        {
            subscription.Status = SubscriptionStatus.Active;
        }

        await _context.SaveChangesAsync();

        var dto = await MapToDtoAsync(subscription);
        return ApiResponse<SubscriptionDto>.Ok(dto, "Subscription extended successfully");
    }

    public async Task<ApiResponse<SubscriptionDto>> CancelSubscriptionAsync(Guid id, CancelSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (subscription == null)
        {
            return ApiResponse<SubscriptionDto>.Error("Subscription not found");
        }

        // Already cancelled
        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            return ApiResponse<SubscriptionDto>.Error("Subscription is already cancelled");
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.CancelReason = request.CancelReason;

        await _context.SaveChangesAsync();

        var dto = await MapToDtoAsync(subscription);
        return ApiResponse<SubscriptionDto>.Ok(dto, "Subscription cancelled successfully");
    }

    public async Task<ApiResponse<SubscriptionDto>> MarkPaidAsync(Guid id, MarkPaidRequest request)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (subscription == null)
        {
            return ApiResponse<SubscriptionDto>.Error("Subscription not found");
        }

        if (subscription.IsPaid)
        {
            return ApiResponse<SubscriptionDto>.Error("Subscription is already marked as paid");
        }

        subscription.IsPaid = true;
        subscription.PaidAt = request.PaidAt ?? DateTime.UtcNow;
        subscription.PaymentMethod = request.PaymentMethod;
        subscription.PaymentReference = request.PaymentReference;

        await _context.SaveChangesAsync();

        var dto = await MapToDtoAsync(subscription);
        return ApiResponse<SubscriptionDto>.Ok(dto, "Subscription marked as paid successfully");
    }

    private async Task<SubscriptionDto> MapToDtoAsync(Subscription subscription)
    {
        // Ensure tenant is loaded
        if (subscription.Tenant == null)
        {
            await _context.Entry(subscription)
                .Reference(s => s.Tenant)
                .LoadAsync();
        }

        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = subscription.Tenant?.Name ?? "Unknown",
            PlanName = subscription.PlanName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            IsPaid = subscription.IsPaid,
            PaidAt = subscription.PaidAt,
            PaymentMethod = subscription.PaymentMethod,
            Status = subscription.Status,
            CancelledAt = subscription.CancelledAt,
            CancelReason = subscription.CancelReason,
            CreatedAt = subscription.CreatedAt
        };
    }
}
