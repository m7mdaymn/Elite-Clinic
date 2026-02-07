using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.FeatureFlags.DTOs;

namespace EliteClinic.Application.Features.Platform.FeatureFlags.Services;

public interface IFeatureFlagService
{
    Task<ApiResponse<FeatureFlagDto>> GetFeatureFlagsByTenantIdAsync(Guid tenantId);
    Task<ApiResponse<FeatureFlagDto>> UpdateFeatureFlagsAsync(Guid tenantId, UpdateFeatureFlagRequest request);
}
