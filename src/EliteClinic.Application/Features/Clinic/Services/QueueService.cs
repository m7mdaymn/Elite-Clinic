using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class QueueService : IQueueService
{
    private readonly EliteClinicDbContext _context;

    public QueueService(EliteClinicDbContext context)
    {
        _context = context;
    }

    // ── Sessions ───────────────────────────────────────────────────

    public async Task<ApiResponse<QueueSessionDto>> OpenSessionAsync(Guid tenantId, CreateQueueSessionRequest request)
    {
        // Validate doctor exists if provided
        if (request.DoctorId.HasValue)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId.Value && d.TenantId == tenantId && !d.IsDeleted);
            if (doctor == null)
                return ApiResponse<QueueSessionDto>.Error("Doctor not found");
        }

        // Check for existing active session for this tenant today (or for specific doctor)
        var today = DateTime.UtcNow.Date;
        var existingQuery = _context.QueueSessions
            .Where(s => s.TenantId == tenantId && s.IsActive && !s.IsDeleted && s.StartedAt.Date == today);

        if (request.DoctorId.HasValue)
            existingQuery = existingQuery.Where(s => s.DoctorId == request.DoctorId.Value);
        else
            existingQuery = existingQuery.Where(s => s.DoctorId == null);

        var existing = await existingQuery.FirstOrDefaultAsync();
        if (existing != null)
            return ApiResponse<QueueSessionDto>.Error("An active session already exists for today");

        var session = new QueueSession
        {
            TenantId = tenantId,
            DoctorId = request.DoctorId,
            Notes = request.Notes,
            IsActive = true,
            StartedAt = DateTime.UtcNow
        };

        _context.QueueSessions.Add(session);
        await _context.SaveChangesAsync();

        var saved = await GetSessionWithIncludes(tenantId, session.Id);
        return ApiResponse<QueueSessionDto>.Created(MapSessionToDto(saved!), "Queue session opened successfully");
    }

    public async Task<ApiResponse<QueueSessionDto>> CloseSessionAsync(Guid tenantId, Guid sessionId)
    {
        var session = await _context.QueueSessions
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId && !s.IsDeleted);

        if (session == null)
            return ApiResponse<QueueSessionDto>.Error("Session not found");

        if (!session.IsActive)
            return ApiResponse<QueueSessionDto>.Error("Session is already closed");

        // Reject close if any tickets are InVisit
        var inVisitTickets = session.Tickets.Where(t => t.Status == TicketStatus.InVisit).ToList();
        if (inVisitTickets.Any())
            return ApiResponse<QueueSessionDto>.Error("Cannot close session while tickets are in-visit. Complete or skip them first.");

        // Mark remaining Waiting/Called tickets as NoShow
        foreach (var ticket in session.Tickets.Where(t => t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called))
        {
            ticket.Status = TicketStatus.NoShow;
        }

        session.IsActive = false;
        session.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<QueueSessionDto>.Ok(MapSessionToDto(session), "Session closed. Remaining tickets marked as no-show.");
    }

    public async Task<ApiResponse<PagedResult<QueueSessionDto>>> GetSessionsAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.QueueSessions
            .Include(s => s.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAt);

        var totalCount = await query.CountAsync();
        var sessions = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<QueueSessionDto>
        {
            Items = sessions.Select(MapSessionToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<QueueSessionDto>>.Ok(result, $"Retrieved {result.Items.Count} session(s)");
    }

    public async Task<ApiResponse<QueueSessionDto>> GetSessionByIdAsync(Guid tenantId, Guid sessionId)
    {
        var session = await GetSessionWithIncludes(tenantId, sessionId);
        if (session == null)
            return ApiResponse<QueueSessionDto>.Error("Session not found");

        return ApiResponse<QueueSessionDto>.Ok(MapSessionToDto(session), "Session retrieved successfully");
    }

    // ── Tickets ────────────────────────────────────────────────────

    public async Task<ApiResponse<QueueTicketDto>> IssueTicketAsync(Guid tenantId, CreateQueueTicketRequest request)
    {
        var session = await _context.QueueSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == tenantId && !s.IsDeleted);

        if (session == null)
            return ApiResponse<QueueTicketDto>.Error("Session not found");

        if (!session.IsActive)
            return ApiResponse<QueueTicketDto>.Error("Session is closed");

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.TenantId == tenantId && !p.IsDeleted);
        if (patient == null)
            return ApiResponse<QueueTicketDto>.Error("Patient not found");

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor == null)
            return ApiResponse<QueueTicketDto>.Error("Doctor not found");

        // Check patient doesn't already have an active ticket
        var activeTicket = await _context.QueueTickets
            .FirstOrDefaultAsync(t => t.PatientId == request.PatientId && t.TenantId == tenantId && !t.IsDeleted
                && (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called || t.Status == TicketStatus.InVisit));
        if (activeTicket != null)
            return ApiResponse<QueueTicketDto>.Error("Patient already has an active ticket");

        // Next ticket number for this session
        var maxTicketNum = await _context.QueueTickets
            .Where(t => t.SessionId == request.SessionId && !t.IsDeleted)
            .MaxAsync(t => (int?)t.TicketNumber) ?? 0;

        var ticket = new QueueTicket
        {
            TenantId = tenantId,
            SessionId = request.SessionId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            DoctorServiceId = request.DoctorServiceId,
            TicketNumber = maxTicketNum + 1,
            Status = TicketStatus.Waiting,
            IsUrgent = false,
            IssuedAt = DateTime.UtcNow,
            Notes = request.Notes
        };

        _context.QueueTickets.Add(ticket);
        await _context.SaveChangesAsync();

        var saved = await GetTicketWithIncludes(tenantId, ticket.Id);
        return ApiResponse<QueueTicketDto>.Created(MapTicketToDto(saved!), "Ticket issued successfully");
    }

    public async Task<ApiResponse<QueueTicketDto>> CallTicketAsync(Guid tenantId, Guid ticketId, Guid callerUserId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status != TicketStatus.Waiting && ticket.Status != TicketStatus.Skipped)
            return ApiResponse<QueueTicketDto>.Error($"Cannot call ticket in {ticket.Status} status. Must be Waiting or Skipped.");

        ticket.Status = TicketStatus.Called;
        ticket.CalledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Patient called successfully");
    }

    public async Task<ApiResponse<QueueTicketDto>> StartVisitFromTicketAsync(Guid tenantId, Guid ticketId, Guid callerUserId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status != TicketStatus.Called)
            return ApiResponse<QueueTicketDto>.Error($"Cannot start visit from {ticket.Status} status. Must be Called.");

        ticket.Status = TicketStatus.InVisit;
        ticket.VisitStartedAt = DateTime.UtcNow;

        // Auto-create visit linked to this ticket
        var visit = new Visit
        {
            TenantId = tenantId,
            QueueTicketId = ticket.Id,
            DoctorId = ticket.DoctorId,
            PatientId = ticket.PatientId,
            Status = VisitStatus.Open,
            StartedAt = DateTime.UtcNow
        };
        _context.Visits.Add(visit);

        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Visit started successfully");
    }

    public async Task<ApiResponse<QueueTicketDto>> FinishTicketAsync(Guid tenantId, Guid ticketId, Guid callerUserId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status != TicketStatus.InVisit)
            return ApiResponse<QueueTicketDto>.Error($"Cannot finish ticket in {ticket.Status} status. Must be InVisit.");

        ticket.Status = TicketStatus.Completed;
        ticket.CompletedAt = DateTime.UtcNow;

        // Also complete the linked visit if it exists and is still Open
        var visit = await _context.Visits
            .FirstOrDefaultAsync(v => v.QueueTicketId == ticketId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit != null && visit.Status == VisitStatus.Open)
        {
            visit.Status = VisitStatus.Completed;
            visit.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Ticket completed successfully");
    }

    public async Task<ApiResponse<QueueTicketDto>> SkipTicketAsync(Guid tenantId, Guid ticketId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status != TicketStatus.Waiting && ticket.Status != TicketStatus.Called)
            return ApiResponse<QueueTicketDto>.Error($"Cannot skip ticket in {ticket.Status} status");

        ticket.Status = TicketStatus.Skipped;
        ticket.SkippedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Ticket skipped");
    }

    public async Task<ApiResponse<QueueTicketDto>> CancelTicketAsync(Guid tenantId, Guid ticketId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status == TicketStatus.InVisit || ticket.Status == TicketStatus.Completed)
            return ApiResponse<QueueTicketDto>.Error($"Cannot cancel ticket in {ticket.Status} status");

        ticket.Status = TicketStatus.Cancelled;
        ticket.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Ticket cancelled");
    }

    public async Task<ApiResponse<QueueTicketDto>> MarkUrgentAsync(Guid tenantId, Guid ticketId)
    {
        var ticket = await GetTicketWithIncludes(tenantId, ticketId);
        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("Ticket not found");

        if (ticket.Status != TicketStatus.Waiting)
            return ApiResponse<QueueTicketDto>.Error("Can only mark waiting tickets as urgent");

        ticket.IsUrgent = true;
        await _context.SaveChangesAsync();

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Ticket marked as urgent");
    }

    public async Task<ApiResponse<List<QueueTicketDto>>> GetTicketsBySessionAsync(Guid tenantId, Guid sessionId)
    {
        var session = await _context.QueueSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId && !s.IsDeleted);
        if (session == null)
            return ApiResponse<List<QueueTicketDto>>.Error("Session not found");

        var tickets = await _context.QueueTickets
            .Include(t => t.Patient)
            .Include(t => t.Doctor)
            .Include(t => t.DoctorService)
            .Where(t => t.SessionId == sessionId && t.TenantId == tenantId && !t.IsDeleted)
            .OrderBy(t => t.IsUrgent ? 0 : 1)
            .ThenBy(t => t.IssuedAt)
            .ToListAsync();

        return ApiResponse<List<QueueTicketDto>>.Ok(
            tickets.Select(MapTicketToDto).ToList(),
            $"Retrieved {tickets.Count} ticket(s)");
    }

    // ── Views ──────────────────────────────────────────────────────

    public async Task<ApiResponse<QueueBoardDto>> GetBoardAsync(Guid tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var sessions = await _context.QueueSessions
            .Include(s => s.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Patient)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.DoctorService)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.StartedAt.Date == today)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.StartedAt)
            .ToListAsync();

        var board = new QueueBoardDto
        {
            Sessions = sessions.Select(s =>
            {
                var tickets = s.Tickets.OrderBy(t => t.IsUrgent ? 0 : 1).ThenBy(t => t.IssuedAt).ToList();
                var current = tickets.FirstOrDefault(t => t.Status == TicketStatus.InVisit || t.Status == TicketStatus.Called);

                return new QueueBoardSessionDto
                {
                    SessionId = s.Id,
                    DoctorId = s.DoctorId,
                    DoctorName = s.Doctor?.Name,
                    IsActive = s.IsActive,
                    WaitingCount = tickets.Count(t => t.Status == TicketStatus.Waiting),
                    CalledCount = tickets.Count(t => t.Status == TicketStatus.Called),
                    InVisitCount = tickets.Count(t => t.Status == TicketStatus.InVisit),
                    CompletedCount = tickets.Count(t => t.Status == TicketStatus.Completed),
                    CurrentTicket = current != null ? MapTicketToDto(current) : null,
                    WaitingTickets = tickets.Where(t => t.Status == TicketStatus.Waiting).Select(MapTicketToDto).ToList()
                };
            }).ToList()
        };

        return ApiResponse<QueueBoardDto>.Ok(board, "Board retrieved successfully");
    }

    public async Task<ApiResponse<QueueBoardSessionDto>> GetMyQueueAsync(Guid tenantId, Guid doctorUserId)
    {
        // Find doctor by UserId
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor == null)
            return ApiResponse<QueueBoardSessionDto>.Error("Doctor not found");

        var today = DateTime.UtcNow.Date;
        var session = await _context.QueueSessions
            .Include(s => s.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Patient)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.DoctorService)
            .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.IsActive && s.StartedAt.Date == today)
            .FirstOrDefaultAsync(s => s.DoctorId == doctor.Id || s.DoctorId == null);

        if (session == null)
            return ApiResponse<QueueBoardSessionDto>.Error("No active session found for today");

        // Filter to only this doctor's tickets if session is tenant-wide
        var tickets = session.Tickets
            .Where(t => t.DoctorId == doctor.Id)
            .OrderBy(t => t.IsUrgent ? 0 : 1)
            .ThenBy(t => t.IssuedAt)
            .ToList();

        var current = tickets.FirstOrDefault(t => t.Status == TicketStatus.InVisit || t.Status == TicketStatus.Called);

        var dto = new QueueBoardSessionDto
        {
            SessionId = session.Id,
            DoctorId = doctor.Id,
            DoctorName = doctor.Name,
            IsActive = session.IsActive,
            WaitingCount = tickets.Count(t => t.Status == TicketStatus.Waiting),
            CalledCount = tickets.Count(t => t.Status == TicketStatus.Called),
            InVisitCount = tickets.Count(t => t.Status == TicketStatus.InVisit),
            CompletedCount = tickets.Count(t => t.Status == TicketStatus.Completed),
            CurrentTicket = current != null ? MapTicketToDto(current) : null,
            WaitingTickets = tickets.Where(t => t.Status == TicketStatus.Waiting).Select(MapTicketToDto).ToList()
        };

        return ApiResponse<QueueBoardSessionDto>.Ok(dto, "Doctor queue retrieved successfully");
    }

    public async Task<ApiResponse<QueueTicketDto>> GetMyTicketAsync(Guid tenantId, Guid patientUserId)
    {
        // Find patient by UserId
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == patientUserId && p.TenantId == tenantId && !p.IsDeleted && p.IsDefault);
        if (patient == null)
            return ApiResponse<QueueTicketDto>.Error("Patient not found");

        var ticket = await _context.QueueTickets
            .Include(t => t.Patient)
            .Include(t => t.Doctor)
            .Include(t => t.DoctorService)
            .Where(t => t.PatientId == patient.Id && t.TenantId == tenantId && !t.IsDeleted
                && (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called || t.Status == TicketStatus.InVisit))
            .OrderByDescending(t => t.IssuedAt)
            .FirstOrDefaultAsync();

        if (ticket == null)
            return ApiResponse<QueueTicketDto>.Error("No active ticket found");

        return ApiResponse<QueueTicketDto>.Ok(MapTicketToDto(ticket), "Ticket retrieved successfully");
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<QueueSession?> GetSessionWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.QueueSessions
            .Include(s => s.Doctor)
            .Include(s => s.Tickets.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId && !s.IsDeleted);
    }

    private async Task<QueueTicket?> GetTicketWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.QueueTickets
            .Include(t => t.Patient)
            .Include(t => t.Doctor)
            .Include(t => t.DoctorService)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && !t.IsDeleted);
    }

    private static QueueSessionDto MapSessionToDto(QueueSession s)
    {
        return new QueueSessionDto
        {
            Id = s.Id,
            DoctorId = s.DoctorId,
            DoctorName = s.Doctor?.Name,
            StartedAt = s.StartedAt,
            ClosedAt = s.ClosedAt,
            IsActive = s.IsActive,
            Notes = s.Notes,
            TotalTickets = s.Tickets?.Count(t => !t.IsDeleted) ?? 0,
            WaitingCount = s.Tickets?.Count(t => !t.IsDeleted && t.Status == TicketStatus.Waiting) ?? 0,
            CompletedCount = s.Tickets?.Count(t => !t.IsDeleted && t.Status == TicketStatus.Completed) ?? 0,
            CreatedAt = s.CreatedAt
        };
    }

    private static QueueTicketDto MapTicketToDto(QueueTicket t)
    {
        return new QueueTicketDto
        {
            Id = t.Id,
            SessionId = t.SessionId,
            PatientId = t.PatientId,
            PatientName = t.Patient?.Name ?? string.Empty,
            DoctorId = t.DoctorId,
            DoctorName = t.Doctor?.Name ?? string.Empty,
            DoctorServiceId = t.DoctorServiceId,
            ServiceName = t.DoctorService?.ServiceName,
            TicketNumber = t.TicketNumber,
            Status = t.Status,
            IsUrgent = t.IsUrgent,
            IssuedAt = t.IssuedAt,
            CalledAt = t.CalledAt,
            VisitStartedAt = t.VisitStartedAt,
            CompletedAt = t.CompletedAt,
            Notes = t.Notes
        };
    }
}
