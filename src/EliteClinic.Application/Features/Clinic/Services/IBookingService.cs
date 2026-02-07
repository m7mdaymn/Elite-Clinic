using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IBookingService
{
    Task<ApiResponse<BookingDto>> CreateAsync(Guid tenantId, Guid patientUserId, CreateBookingRequest request);
    Task<ApiResponse<BookingDto>> CancelAsync(Guid tenantId, Guid bookingId, Guid callerUserId, CancelBookingRequest request);
    Task<ApiResponse<BookingDto>> RescheduleAsync(Guid tenantId, Guid bookingId, Guid callerUserId, RescheduleBookingRequest request);
    Task<ApiResponse<BookingDto>> GetByIdAsync(Guid tenantId, Guid bookingId);
    Task<ApiResponse<PagedResult<BookingDto>>> GetAllAsync(Guid tenantId, Guid? patientId, Guid? doctorId, string? status, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<List<BookingDto>>> GetMyBookingsAsync(Guid tenantId, Guid patientUserId);
}
