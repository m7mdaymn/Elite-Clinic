using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Platform.Tenants.DTOs;

namespace EliteClinic.Application.Features.Platform.Tenants.Services;

public interface ITenantService
{
    Task<ApiResponse<TenantDetailDto>> CreateTenantAsync(CreateTenantRequest request);
    Task<ApiResponse<PagedResult<TenantDto>>> GetAllTenantsAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null);
    Task<ApiResponse<TenantDetailDto>> GetTenantByIdAsync(Guid id);
    Task<ApiResponse<TenantDetailDto>> UpdateTenantAsync(Guid id, UpdateTenantRequest request);
    Task<ApiResponse<TenantDetailDto>> ActivateTenantAsync(Guid id);
    Task<ApiResponse<TenantDetailDto>> SuspendTenantAsync(Guid id);
    Task<ApiResponse<TenantDetailDto>> BlockTenantAsync(Guid id);
    Task<ApiResponse<bool>> DeleteTenantAsync(Guid id);
}
