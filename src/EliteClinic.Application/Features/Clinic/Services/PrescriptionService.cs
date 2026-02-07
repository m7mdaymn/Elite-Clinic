using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly EliteClinicDbContext _context;

    public PrescriptionService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PrescriptionDto>> CreateAsync(Guid tenantId, Guid visitId, CreatePrescriptionRequest request, Guid callerUserId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<PrescriptionDto>.Error("Visit not found");

        if (visit.Status != VisitStatus.Open)
            return ApiResponse<PrescriptionDto>.Error("Cannot add prescriptions to a completed visit");

        var prescription = new Prescription
        {
            TenantId = tenantId,
            VisitId = visitId,
            MedicationName = request.MedicationName,
            Dosage = request.Dosage,
            Frequency = request.Frequency,
            Duration = request.Duration,
            Instructions = request.Instructions
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return ApiResponse<PrescriptionDto>.Created(MapToDto(prescription), "Prescription added successfully");
    }

    public async Task<ApiResponse<PrescriptionDto>> UpdateAsync(Guid tenantId, Guid visitId, Guid prescriptionId,
        UpdatePrescriptionRequest request, Guid callerUserId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<PrescriptionDto>.Error("Visit not found");

        // Same-day check for doctor
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == callerUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor != null && visit.StartedAt.Date != DateTime.UtcNow.Date)
            return ApiResponse<PrescriptionDto>.Error("Can only edit prescriptions from today's visits");

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.VisitId == visitId && p.TenantId == tenantId && !p.IsDeleted);
        if (prescription == null)
            return ApiResponse<PrescriptionDto>.Error("Prescription not found");

        prescription.MedicationName = request.MedicationName;
        prescription.Dosage = request.Dosage;
        prescription.Frequency = request.Frequency;
        prescription.Duration = request.Duration;
        prescription.Instructions = request.Instructions;

        await _context.SaveChangesAsync();

        return ApiResponse<PrescriptionDto>.Ok(MapToDto(prescription), "Prescription updated successfully");
    }

    public async Task<ApiResponse> DeleteAsync(Guid tenantId, Guid visitId, Guid prescriptionId, Guid callerUserId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse.Error("Visit not found");

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == callerUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor != null && visit.StartedAt.Date != DateTime.UtcNow.Date)
            return ApiResponse.Error("Can only delete prescriptions from today's visits");

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.VisitId == visitId && p.TenantId == tenantId && !p.IsDeleted);
        if (prescription == null)
            return ApiResponse.Error("Prescription not found");

        _context.Prescriptions.Remove(prescription); // soft-delete via SaveChangesAsync override
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Prescription deleted successfully");
    }

    public async Task<ApiResponse<List<PrescriptionDto>>> GetByVisitAsync(Guid tenantId, Guid visitId)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId && v.TenantId == tenantId && !v.IsDeleted);
        if (visit == null)
            return ApiResponse<List<PrescriptionDto>>.Error("Visit not found");

        var prescriptions = await _context.Prescriptions
            .Where(p => p.VisitId == visitId && p.TenantId == tenantId && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<PrescriptionDto>>.Ok(
            prescriptions.Select(MapToDto).ToList(),
            $"Retrieved {prescriptions.Count} prescription(s)");
    }

    private static PrescriptionDto MapToDto(Prescription p)
    {
        return new PrescriptionDto
        {
            Id = p.Id,
            VisitId = p.VisitId,
            MedicationName = p.MedicationName,
            Dosage = p.Dosage,
            Frequency = p.Frequency,
            Duration = p.Duration,
            Instructions = p.Instructions,
            CreatedAt = p.CreatedAt
        };
    }
}
