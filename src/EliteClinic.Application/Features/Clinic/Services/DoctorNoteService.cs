using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class DoctorNoteService : IDoctorNoteService
{
    private readonly EliteClinicDbContext _context;

    public DoctorNoteService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DoctorNoteDto>> CreateAsync(Guid tenantId, Guid doctorUserId, CreateDoctorNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return ApiResponse<DoctorNoteDto>.Error("Message is required");

        // Find doctor by userId
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor == null)
            return ApiResponse<DoctorNoteDto>.Error("Doctor profile not found");

        var note = new DoctorNote
        {
            TenantId = tenantId,
            DoctorId = doctor.Id,
            Message = request.Message,
            IsRead = false
        };

        _context.DoctorNotes.Add(note);
        await _context.SaveChangesAsync();

        var saved = await GetNoteWithIncludes(tenantId, note.Id);
        return ApiResponse<DoctorNoteDto>.Created(MapToDto(saved!), "Note sent to reception");
    }

    public async Task<ApiResponse<List<DoctorNoteDto>>> GetUnreadAsync(Guid tenantId)
    {
        var notes = await _context.DoctorNotes
            .Include(n => n.Doctor)
            .Where(n => n.TenantId == tenantId && !n.IsDeleted && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<DoctorNoteDto>>.Ok(notes.Select(MapToDto).ToList(), $"Retrieved {notes.Count} unread note(s)");
    }

    public async Task<ApiResponse<PagedResult<DoctorNoteDto>>> GetAllAsync(Guid tenantId, bool? unreadOnly, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.DoctorNotes
            .Include(n => n.Doctor)
            .Where(n => n.TenantId == tenantId && !n.IsDeleted);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync();
        var notes = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<DoctorNoteDto>
        {
            Items = notes.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<DoctorNoteDto>>.Ok(result, $"Retrieved {result.Items.Count} note(s)");
    }

    public async Task<ApiResponse<DoctorNoteDto>> MarkAsReadAsync(Guid tenantId, Guid noteId, Guid readerUserId)
    {
        var note = await GetNoteWithIncludes(tenantId, noteId);
        if (note == null)
            return ApiResponse<DoctorNoteDto>.Error("Note not found");

        if (note.IsRead)
            return ApiResponse<DoctorNoteDto>.Error("Note is already read");

        note.IsRead = true;
        note.ReadAt = DateTime.UtcNow;
        note.ReadByUserId = readerUserId;
        await _context.SaveChangesAsync();

        var updated = await GetNoteWithIncludes(tenantId, noteId);
        return ApiResponse<DoctorNoteDto>.Ok(MapToDto(updated!), "Note marked as read");
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<DoctorNote?> GetNoteWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.DoctorNotes
            .Include(n => n.Doctor)
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId && !n.IsDeleted);
    }

    private static DoctorNoteDto MapToDto(DoctorNote n) => new()
    {
        Id = n.Id,
        DoctorId = n.DoctorId,
        DoctorName = n.Doctor?.Name ?? string.Empty,
        Message = n.Message,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        ReadByUserId = n.ReadByUserId,
        CreatedAt = n.CreatedAt
    };
}
