using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IPrescriptionService
{
    Task<ApiResponse<PrescriptionDto>> CreateAsync(Guid tenantId, Guid visitId, CreatePrescriptionRequest request, Guid callerUserId);
    Task<ApiResponse<PrescriptionDto>> UpdateAsync(Guid tenantId, Guid visitId, Guid prescriptionId, UpdatePrescriptionRequest request, Guid callerUserId);
    Task<ApiResponse> DeleteAsync(Guid tenantId, Guid visitId, Guid prescriptionId, Guid callerUserId);
    Task<ApiResponse<List<PrescriptionDto>>> GetByVisitAsync(Guid tenantId, Guid visitId);
}
