using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class LabRequestService : ILabRequestService
{
    private readonly EliteClinicDbContext _context;

    public LabRequestService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<LabRequestDto>> CreateAsync(Guid tenantId, Guid visitId, CreateLabRequestRequest request, Guid callerUserId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<LabRequestDto>.Error("Visit not found");

        if (visit.Status != VisitStatus.Open)
            return ApiResponse<LabRequestDto>.Error("Cannot add lab requests to a completed visit");

        var labRequest = new LabRequest
        {
            TenantId = tenantId,
            VisitId = visitId,
            TestName = request.TestName,
            Type = request.Type,
            Notes = request.Notes,
            IsUrgent = request.IsUrgent
        };

        _context.LabRequests.Add(labRequest);
        await _context.SaveChangesAsync();

        return ApiResponse<LabRequestDto>.Created(MapToDto(labRequest), "Lab request added successfully");
    }

    public async Task<ApiResponse<LabRequestDto>> UpdateAsync(Guid tenantId, Guid visitId, Guid labId,
        UpdateLabRequestRequest request, Guid callerUserId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<LabRequestDto>.Error("Visit not found");

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == callerUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor != null && visit.StartedAt.Date != DateTime.UtcNow.Date)
            return ApiResponse<LabRequestDto>.Error("Can only edit lab requests from today's visits");

        var labRequest = await _context.LabRequests
            .FirstOrDefaultAsync(l => l.Id == labId && l.VisitId == visitId && l.TenantId == tenantId && !l.IsDeleted);
        if (labRequest == null)
            return ApiResponse<LabRequestDto>.Error("Lab request not found");

        labRequest.TestName = request.TestName;
        labRequest.Type = request.Type;
        labRequest.Notes = request.Notes;
        labRequest.IsUrgent = request.IsUrgent;

        await _context.SaveChangesAsync();

        return ApiResponse<LabRequestDto>.Ok(MapToDto(labRequest), "Lab request updated successfully");
    }

    public async Task<ApiResponse<LabRequestDto>> AddResultAsync(Guid tenantId, Guid visitId, Guid labId, AddLabResultRequest request)
    {
        var labRequest = await _context.LabRequests
            .FirstOrDefaultAsync(l => l.Id == labId && l.VisitId == visitId && l.TenantId == tenantId && !l.IsDeleted);
        if (labRequest == null)
            return ApiResponse<LabRequestDto>.Error("Lab request not found");

        labRequest.ResultText = request.ResultText;
        labRequest.ResultReceivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<LabRequestDto>.Ok(MapToDto(labRequest), "Lab result recorded successfully");
    }

    public async Task<ApiResponse<List<LabRequestDto>>> GetByVisitAsync(Guid tenantId, Guid visitId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<List<LabRequestDto>>.Error("Visit not found");

        var labs = await _context.LabRequests
            .Where(l => l.VisitId == visitId && l.TenantId == tenantId && !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<LabRequestDto>>.Ok(
            labs.Select(MapToDto).ToList(),
            $"Retrieved {labs.Count} lab request(s)");
    }

    private static LabRequestDto MapToDto(LabRequest l)
    {
        return new LabRequestDto
        {
            Id = l.Id,
            VisitId = l.VisitId,
            TestName = l.TestName,
            Type = l.Type,
            Notes = l.Notes,
            IsUrgent = l.IsUrgent,
            ResultText = l.ResultText,
            ResultReceivedAt = l.ResultReceivedAt,
            CreatedAt = l.CreatedAt
        };
    }
}
