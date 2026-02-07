using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IStaffService
{
    Task<ApiResponse<StaffDto>> CreateStaffAsync(Guid tenantId, CreateStaffRequest request);
    Task<ApiResponse<PagedResult<StaffDto>>> GetAllStaffAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<StaffDto>> GetStaffByIdAsync(Guid tenantId, Guid id);
    Task<ApiResponse<StaffDto>> UpdateStaffAsync(Guid tenantId, Guid id, UpdateStaffRequest request);
    Task<ApiResponse<StaffDto>> EnableStaffAsync(Guid tenantId, Guid id);
    Task<ApiResponse<StaffDto>> DisableStaffAsync(Guid tenantId, Guid id);
}
