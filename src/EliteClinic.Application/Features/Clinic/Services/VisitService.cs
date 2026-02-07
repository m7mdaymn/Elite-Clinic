using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class VisitService : IVisitService
{
    private readonly EliteClinicDbContext _context;

    public VisitService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<VisitDto>> CreateVisitAsync(Guid tenantId, CreateVisitRequest request)
    {
        // Validate doctor
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor == null)
            return ApiResponse<VisitDto>.Error("Doctor not found");

        // Validate patient
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId && p.TenantId == tenantId && !p.IsDeleted);
        if (patient == null)
            return ApiResponse<VisitDto>.Error("Patient not found");

        // If from ticket, validate and link
        if (request.QueueTicketId.HasValue)
        {
            var ticket = await _context.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == request.QueueTicketId.Value && t.TenantId == tenantId && !t.IsDeleted);
            if (ticket == null)
                return ApiResponse<VisitDto>.Error("Queue ticket not found");

            // Check no visit already linked
            var existingVisit = await _context.Visits
                .FirstOrDefaultAsync(v => v.QueueTicketId == request.QueueTicketId.Value && !v.IsDeleted);
            if (existingVisit != null)
                return ApiResponse<VisitDto>.Error("A visit already exists for this ticket");

            // Update ticket status
            if (ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Waiting)
            {
                ticket.Status = TicketStatus.InVisit;
                ticket.VisitStartedAt = DateTime.UtcNow;
            }
        }

        var visit = new Visit
        {
            TenantId = tenantId,
            QueueTicketId = request.QueueTicketId,
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            Status = VisitStatus.Open,
            Complaint = request.Complaint,
            Notes = request.Notes,
            StartedAt = DateTime.UtcNow
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        var saved = await GetVisitWithIncludes(tenantId, visit.Id);
        return ApiResponse<VisitDto>.Created(MapVisitToDto(saved!), "Visit created successfully");
    }

    public async Task<ApiResponse<VisitDto>> UpdateVisitAsync(Guid tenantId, Guid visitId, UpdateVisitRequest request, Guid callerUserId)
    {
        var visit = await GetVisitWithIncludes(tenantId, visitId);
        if (visit == null)
            return ApiResponse<VisitDto>.Error("Visit not found");

        if (visit.Status != VisitStatus.Open)
            return ApiResponse<VisitDto>.Error("Cannot update a completed visit");

        // Same-day edit check for doctors
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == callerUserId && d.TenantId == tenantId && !d.IsDeleted);
        if (doctor != null && visit.DoctorId != doctor.Id)
            return ApiResponse<VisitDto>.Error("You can only edit your own visits");
        if (doctor != null && visit.StartedAt.Date != DateTime.UtcNow.Date)
            return ApiResponse<VisitDto>.Error("You can only edit visits from today");

        visit.Complaint = request.Complaint;
        visit.Diagnosis = request.Diagnosis;
        visit.Notes = request.Notes;
        visit.BloodPressureSystolic = request.BloodPressureSystolic;
        visit.BloodPressureDiastolic = request.BloodPressureDiastolic;
        visit.HeartRate = request.HeartRate;
        visit.Temperature = request.Temperature;
        visit.Weight = request.Weight;
        visit.Height = request.Height;
        visit.BMI = request.BMI;
        visit.BloodSugar = request.BloodSugar;
        visit.OxygenSaturation = request.OxygenSaturation;
        visit.RespiratoryRate = request.RespiratoryRate;
        visit.FollowUpDate = request.FollowUpDate;

        await _context.SaveChangesAsync();

        var updated = await GetVisitWithIncludes(tenantId, visitId);
        return ApiResponse<VisitDto>.Ok(MapVisitToDto(updated!), "Visit updated successfully");
    }

    public async Task<ApiResponse<VisitDto>> CompleteVisitAsync(Guid tenantId, Guid visitId, CompleteVisitRequest request, Guid callerUserId)
    {
        var visit = await GetVisitWithIncludes(tenantId, visitId);
        if (visit == null)
            return ApiResponse<VisitDto>.Error("Visit not found");

        if (visit.Status != VisitStatus.Open)
            return ApiResponse<VisitDto>.Error("Visit is already completed");

        visit.Status = VisitStatus.Completed;
        visit.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Diagnosis))
            visit.Diagnosis = request.Diagnosis;
        if (!string.IsNullOrEmpty(request.Notes))
            visit.Notes = request.Notes;

        // Also complete the linked ticket if it exists
        if (visit.QueueTicketId.HasValue)
        {
            var ticket = await _context.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == visit.QueueTicketId.Value && !t.IsDeleted);
            if (ticket != null && ticket.Status == TicketStatus.InVisit)
            {
                ticket.Status = TicketStatus.Completed;
                ticket.CompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var updated = await GetVisitWithIncludes(tenantId, visitId);
        return ApiResponse<VisitDto>.Ok(MapVisitToDto(updated!), "Visit completed successfully");
    }

    public async Task<ApiResponse<VisitDto>> GetVisitByIdAsync(Guid tenantId, Guid visitId)
    {
        var visit = await GetVisitWithIncludes(tenantId, visitId);
        if (visit == null)
            return ApiResponse<VisitDto>.Error("Visit not found");

        return ApiResponse<VisitDto>.Ok(MapVisitToDto(visit), "Visit retrieved successfully");
    }

    public async Task<ApiResponse<PagedResult<VisitDto>>> GetPatientVisitsAsync(Guid tenantId, Guid patientId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Visits
            .Include(v => v.Doctor)
            .Include(v => v.Patient)
            .Include(v => v.Prescriptions.Where(p => !p.IsDeleted))
            .Include(v => v.LabRequests.Where(l => !l.IsDeleted))
            .Include(v => v.Invoice)
            .Where(v => v.PatientId == patientId && v.TenantId == tenantId)
            .OrderByDescending(v => v.StartedAt);

        var totalCount = await query.CountAsync();
        var visits = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<VisitDto>
        {
            Items = visits.Select(MapVisitToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<VisitDto>>.Ok(result, $"Retrieved {result.Items.Count} visit(s)");
    }

    public async Task<ApiResponse<PatientSummaryDto>> GetPatientSummaryAsync(Guid tenantId, Guid patientId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId && p.TenantId == tenantId && !p.IsDeleted);
        if (patient == null)
            return ApiResponse<PatientSummaryDto>.Error("Patient not found");

        var totalVisits = await _context.Visits
            .CountAsync(v => v.PatientId == patientId && v.TenantId == tenantId && !v.IsDeleted);

        var recentVisits = await _context.Visits
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patientId && v.TenantId == tenantId && !v.IsDeleted)
            .OrderByDescending(v => v.StartedAt)
            .Take(5)
            .ToListAsync();

        var summary = new PatientSummaryDto
        {
            PatientId = patient.Id,
            Name = patient.Name,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender.ToString(),
            TotalVisits = totalVisits,
            RecentVisits = recentVisits.Select(v => new VisitSummaryDto
            {
                Id = v.Id,
                DoctorName = v.Doctor?.Name ?? string.Empty,
                Complaint = v.Complaint,
                Diagnosis = v.Diagnosis,
                StartedAt = v.StartedAt,
                CompletedAt = v.CompletedAt
            }).ToList()
        };

        return ApiResponse<PatientSummaryDto>.Ok(summary, "Patient summary retrieved successfully");
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<Visit?> GetVisitWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.Visits
            .Include(v => v.Doctor)
            .Include(v => v.Patient)
            .Include(v => v.Prescriptions.Where(p => !p.IsDeleted))
            .Include(v => v.LabRequests.Where(l => !l.IsDeleted))
            .Include(v => v.Invoice)
                .ThenInclude(i => i!.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId && !v.IsDeleted);
    }

    internal static VisitDto MapVisitToDto(Visit v)
    {
        return new VisitDto
        {
            Id = v.Id,
            QueueTicketId = v.QueueTicketId,
            DoctorId = v.DoctorId,
            DoctorName = v.Doctor?.Name ?? string.Empty,
            PatientId = v.PatientId,
            PatientName = v.Patient?.Name ?? string.Empty,
            Status = v.Status,
            Complaint = v.Complaint,
            Diagnosis = v.Diagnosis,
            Notes = v.Notes,
            BloodPressureSystolic = v.BloodPressureSystolic,
            BloodPressureDiastolic = v.BloodPressureDiastolic,
            HeartRate = v.HeartRate,
            Temperature = v.Temperature,
            Weight = v.Weight,
            Height = v.Height,
            BMI = v.BMI,
            BloodSugar = v.BloodSugar,
            OxygenSaturation = v.OxygenSaturation,
            RespiratoryRate = v.RespiratoryRate,
            FollowUpDate = v.FollowUpDate,
            StartedAt = v.StartedAt,
            CompletedAt = v.CompletedAt,
            Prescriptions = v.Prescriptions?.Where(p => !p.IsDeleted).Select(p => new PrescriptionDto
            {
                Id = p.Id,
                VisitId = p.VisitId,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Frequency = p.Frequency,
                Duration = p.Duration,
                Instructions = p.Instructions,
                CreatedAt = p.CreatedAt
            }).ToList() ?? new(),
            LabRequests = v.LabRequests?.Where(l => !l.IsDeleted).Select(l => new LabRequestDto
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
            }).ToList() ?? new(),
            Invoice = v.Invoice != null && !v.Invoice.IsDeleted ? new InvoiceDto
            {
                Id = v.Invoice.Id,
                VisitId = v.Invoice.VisitId,
                PatientId = v.Invoice.PatientId,
                PatientName = v.Patient?.Name ?? string.Empty,
                DoctorId = v.Invoice.DoctorId,
                DoctorName = v.Doctor?.Name ?? string.Empty,
                Amount = v.Invoice.Amount,
                PaidAmount = v.Invoice.PaidAmount,
                RemainingAmount = v.Invoice.RemainingAmount,
                Status = v.Invoice.Status,
                Notes = v.Invoice.Notes,
                Payments = v.Invoice.Payments?.Where(p => !p.IsDeleted).Select(p => new PaymentDto
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    PaidAt = p.PaidAt,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt
                }).ToList() ?? new(),
                CreatedAt = v.Invoice.CreatedAt
            } : null,
            CreatedAt = v.CreatedAt
        };
    }
}
