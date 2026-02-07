using EliteClinic.Domain.Enums;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Booking DTOs ──────────────────────────────────────────────────

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? DoctorServiceId { get; set; }
    public string? ServiceName { get; set; }
    public DateTime BookingDate { get; set; }
    public string BookingTime { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? QueueTicketId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingRequest
{
    public Guid DoctorId { get; set; }
    public Guid? DoctorServiceId { get; set; }
    public DateTime BookingDate { get; set; }
    public string BookingTime { get; set; } = string.Empty;  // HH:mm format
    public string? Notes { get; set; }
}

public class CancelBookingRequest
{
    public string? CancellationReason { get; set; }
}

public class RescheduleBookingRequest
{
    public DateTime BookingDate { get; set; }
    public string BookingTime { get; set; } = string.Empty;  // HH:mm format
}
