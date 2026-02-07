using EliteClinic.Domain.Enums;

namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Queue Session DTOs ────────────────────────────────────────────

public class QueueSessionDto
{
    public Guid Id { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public int TotalTickets { get; set; }
    public int WaitingCount { get; set; }
    public int CompletedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateQueueSessionRequest
{
    public Guid? DoctorId { get; set; }
    public string? Notes { get; set; }
}

// ─── Queue Ticket DTOs ─────────────────────────────────────────────

public class QueueTicketDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? DoctorServiceId { get; set; }
    public string? ServiceName { get; set; }
    public int TicketNumber { get; set; }
    public TicketStatus Status { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? CalledAt { get; set; }
    public DateTime? VisitStartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateQueueTicketRequest
{
    public Guid SessionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? DoctorServiceId { get; set; }
    public string? Notes { get; set; }
}

// ─── Queue Board DTOs ──────────────────────────────────────────────

public class QueueBoardDto
{
    public List<QueueBoardSessionDto> Sessions { get; set; } = new();
}

public class QueueBoardSessionDto
{
    public Guid SessionId { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public bool IsActive { get; set; }
    public int WaitingCount { get; set; }
    public int CalledCount { get; set; }
    public int InVisitCount { get; set; }
    public int CompletedCount { get; set; }
    public QueueTicketDto? CurrentTicket { get; set; }
    public List<QueueTicketDto> WaitingTickets { get; set; } = new();
}
