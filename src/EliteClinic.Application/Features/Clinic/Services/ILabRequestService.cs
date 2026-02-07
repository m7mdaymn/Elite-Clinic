using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface ILabRequestService
{
    Task<ApiResponse<LabRequestDto>> CreateAsync(Guid tenantId, Guid visitId, CreateLabRequestRequest request, Guid callerUserId);
    Task<ApiResponse<LabRequestDto>> UpdateAsync(Guid tenantId, Guid visitId, Guid labId, UpdateLabRequestRequest request, Guid callerUserId);
    Task<ApiResponse<LabRequestDto>> AddResultAsync(Guid tenantId, Guid visitId, Guid labId, AddLabResultRequest request);
    Task<ApiResponse<List<LabRequestDto>>> GetByVisitAsync(Guid tenantId, Guid visitId);
}
