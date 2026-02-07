using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.FeatureFlags.DTOs;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Platform.FeatureFlags.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly EliteClinicDbContext _context;

    public FeatureFlagService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<FeatureFlagDto>> GetFeatureFlagsByTenantIdAsync(Guid tenantId)
    {
        // Validate tenant exists
        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (!tenantExists)
        {
            return ApiResponse<FeatureFlagDto>.Error("Tenant not found");
        }

        var featureFlags = await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && !f.IsDeleted);

        if (featureFlags == null)
        {
            return ApiResponse<FeatureFlagDto>.Error("Feature flags not found for this tenant");
        }

        var dto = MapToDto(featureFlags);
        return ApiResponse<FeatureFlagDto>.Ok(dto);
    }

    public async Task<ApiResponse<FeatureFlagDto>> UpdateFeatureFlagsAsync(Guid tenantId, UpdateFeatureFlagRequest request)
    {
        // Validate tenant exists
        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (!tenantExists)
        {
            return ApiResponse<FeatureFlagDto>.Error("Tenant not found");
        }

        var featureFlags = await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && !f.IsDeleted);

        if (featureFlags == null)
        {
            return ApiResponse<FeatureFlagDto>.Error("Feature flags not found for this tenant");
        }

        featureFlags.OnlineBooking = request.OnlineBooking;
        featureFlags.WhatsappAutomation = request.WhatsappAutomation;
        featureFlags.PwaNotifications = request.PwaNotifications;
        featureFlags.ExpensesModule = request.ExpensesModule;
        featureFlags.AdvancedMedicalTemplates = request.AdvancedMedicalTemplates;
        featureFlags.Ratings = request.Ratings;
        featureFlags.Export = request.Export;

        await _context.SaveChangesAsync();

        var dto = MapToDto(featureFlags);
        return ApiResponse<FeatureFlagDto>.Ok(dto, "Feature flags updated successfully");
    }

    private FeatureFlagDto MapToDto(Domain.Entities.TenantFeatureFlag featureFlags) => new()
    {
        Id = featureFlags.Id,
        TenantId = featureFlags.TenantId,
        OnlineBooking = featureFlags.OnlineBooking,
        WhatsappAutomation = featureFlags.WhatsappAutomation,
        PwaNotifications = featureFlags.PwaNotifications,
        ExpensesModule = featureFlags.ExpensesModule,
        AdvancedMedicalTemplates = featureFlags.AdvancedMedicalTemplates,
        Ratings = featureFlags.Ratings,
        Export = featureFlags.Export
    };
}
