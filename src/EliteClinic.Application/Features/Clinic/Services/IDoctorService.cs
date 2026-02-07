using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IDoctorService
{
    Task<ApiResponse<DoctorDto>> CreateDoctorAsync(Guid tenantId, CreateDoctorRequest request);
    Task<ApiResponse<PagedResult<DoctorDto>>> GetAllDoctorsAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<DoctorDto>> GetDoctorByIdAsync(Guid tenantId, Guid id);
    Task<ApiResponse<DoctorDto>> UpdateDoctorAsync(Guid tenantId, Guid id, UpdateDoctorRequest request);
    Task<ApiResponse<DoctorDto>> EnableDoctorAsync(Guid tenantId, Guid id);
    Task<ApiResponse<DoctorDto>> DisableDoctorAsync(Guid tenantId, Guid id);
    Task<ApiResponse<List<DoctorServiceDto>>> UpdateServicesAsync(Guid tenantId, Guid doctorId, UpdateDoctorServicesRequest request);
    Task<ApiResponse<DoctorVisitFieldConfigDto>> UpdateVisitFieldsAsync(Guid tenantId, Guid doctorId, UpdateVisitFieldsRequest request);
}
