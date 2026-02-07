using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IDoctorNoteService
{
    Task<ApiResponse<DoctorNoteDto>> CreateAsync(Guid tenantId, Guid doctorUserId, CreateDoctorNoteRequest request);
    Task<ApiResponse<List<DoctorNoteDto>>> GetUnreadAsync(Guid tenantId);
    Task<ApiResponse<PagedResult<DoctorNoteDto>>> GetAllAsync(Guid tenantId, bool? unreadOnly, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<DoctorNoteDto>> MarkAsReadAsync(Guid tenantId, Guid noteId, Guid readerUserId);
}
