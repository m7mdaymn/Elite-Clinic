using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class ClinicSettingsService : IClinicSettingsService
{
    private readonly EliteClinicDbContext _context;

    public ClinicSettingsService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ClinicSettingsDto>> GetSettingsAsync(Guid tenantId)
    {
        // Global query filter already constrains by TenantId (via CurrentTenantId)
        // No need for explicit s.TenantId == tenantId (it would create a redundant double filter)
        var settings = await _context.ClinicSettings
            .Include(s => s.WorkingHours.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            return ApiResponse<ClinicSettingsDto>.Error("Clinic settings not found");
        }

        return ApiResponse<ClinicSettingsDto>.Ok(MapToDto(settings), "Clinic settings retrieved successfully");
    }

    public async Task<ApiResponse<ClinicSettingsDto>> UpdateSettingsAsync(Guid tenantId, UpdateClinicSettingsRequest request)
    {
        // Global query filter already constrains by TenantId
        var settings = await _context.ClinicSettings
            .Include(s => s.WorkingHours.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            return ApiResponse<ClinicSettingsDto>.Error("Clinic settings not found");
        }

        settings.ClinicName = request.ClinicName;
        settings.Phone = request.Phone;
        settings.WhatsAppSenderNumber = request.WhatsAppSenderNumber;
        settings.SupportWhatsAppNumber = request.SupportWhatsAppNumber;
        settings.SupportPhoneNumber = request.SupportPhoneNumber;
        settings.Address = request.Address;
        settings.City = request.City;
        settings.LogoUrl = request.LogoUrl;
        settings.BookingEnabled = request.BookingEnabled;
        settings.CancellationWindowHours = request.CancellationWindowHours;

        // Replace working hours if provided
        if (request.WorkingHours != null)
        {
            // Soft-delete existing
            foreach (var existing in settings.WorkingHours)
            {
                existing.IsDeleted = true;
                existing.DeletedAt = DateTime.UtcNow;
            }

            // Add new — use _context.WorkingHours.Add() to force Added state
            // (BaseEntity sets Id = Guid.NewGuid() in constructor, so adding to a
            //  tracked collection makes EF think the entity already exists → UPDATE instead of INSERT)
            foreach (var wh in request.WorkingHours)
            {
                if (!TimeSpan.TryParse(wh.StartTime, out var startTime) ||
                    !TimeSpan.TryParse(wh.EndTime, out var endTime))
                {
                    return ApiResponse<ClinicSettingsDto>.ValidationError(
                        new List<object> { new { field = "WorkingHours", message = $"Invalid time format for {wh.DayOfWeek}. Use HH:mm format." } });
                }

                _context.WorkingHours.Add(new WorkingHour
                {
                    TenantId = tenantId,
                    ClinicSettingsId = settings.Id,
                    DayOfWeek = wh.DayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    IsActive = wh.IsActive
                });
            }
        }

        await _context.SaveChangesAsync();

        // Reload to get fresh data (global query filter handles TenantId)
        var updated = await _context.ClinicSettings
            .Include(s => s.WorkingHours.Where(w => !w.IsDeleted))
            .FirstAsync();

        return ApiResponse<ClinicSettingsDto>.Ok(MapToDto(updated), "Clinic settings updated successfully");
    }

    private static ClinicSettingsDto MapToDto(ClinicSettings settings)
    {
        return new ClinicSettingsDto
        {
            Id = settings.Id,
            TenantId = settings.TenantId,
            ClinicName = settings.ClinicName,
            Phone = settings.Phone,
            WhatsAppSenderNumber = settings.WhatsAppSenderNumber,
            SupportWhatsAppNumber = settings.SupportWhatsAppNumber,
            SupportPhoneNumber = settings.SupportPhoneNumber,
            Address = settings.Address,
            City = settings.City,
            LogoUrl = settings.LogoUrl,
            BookingEnabled = settings.BookingEnabled,
            CancellationWindowHours = settings.CancellationWindowHours,
            WorkingHours = settings.WorkingHours
                .Where(w => !w.IsDeleted)
                .Select(w => new WorkingHourDto
                {
                    Id = w.Id,
                    DayOfWeek = w.DayOfWeek,
                    StartTime = w.StartTime.ToString(@"hh\:mm"),
                    EndTime = w.EndTime.ToString(@"hh\:mm"),
                    IsActive = w.IsActive
                }).ToList()
        };
    }
}
