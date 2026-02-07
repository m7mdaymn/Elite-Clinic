using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IPublicService
{
    Task<ApiResponse<PublicClinicDto>> GetClinicProfileAsync(string tenantSlug);
    Task<ApiResponse<List<PublicDoctorDto>>> GetDoctorsAsync(string tenantSlug);
    Task<ApiResponse<List<PublicDoctorServiceDto>>> GetServicesAsync(string tenantSlug);
    Task<ApiResponse<List<PublicWorkingHourDto>>> GetWorkingHoursAsync(string tenantSlug);
}
