namespace EliteClinic.Application.Features.Clinic.DTOs;

// ─── Doctor Note DTOs ──────────────────────────────────────────────

public class DoctorNoteDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? ReadByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDoctorNoteRequest
{
    public string Message { get; set; } = string.Empty;
}
