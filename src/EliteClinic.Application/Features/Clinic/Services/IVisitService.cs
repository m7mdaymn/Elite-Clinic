using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IVisitService
{
    Task<ApiResponse<VisitDto>> CreateVisitAsync(Guid tenantId, CreateVisitRequest request);
    Task<ApiResponse<VisitDto>> UpdateVisitAsync(Guid tenantId, Guid visitId, UpdateVisitRequest request, Guid callerUserId);
    Task<ApiResponse<VisitDto>> CompleteVisitAsync(Guid tenantId, Guid visitId, CompleteVisitRequest request, Guid callerUserId);
    Task<ApiResponse<VisitDto>> GetVisitByIdAsync(Guid tenantId, Guid visitId);
    Task<ApiResponse<PagedResult<VisitDto>>> GetPatientVisitsAsync(Guid tenantId, Guid patientId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<PatientSummaryDto>> GetPatientSummaryAsync(Guid tenantId, Guid patientId);
}
