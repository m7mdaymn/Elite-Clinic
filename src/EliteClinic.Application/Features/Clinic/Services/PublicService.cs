using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class PublicService : IPublicService
{
    private readonly EliteClinicDbContext _context;

    public PublicService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PublicClinicDto>> GetClinicProfileAsync(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);

        if (tenant == null)
            return ApiResponse<PublicClinicDto>.Ok(null!, "Clinic not found");

        var settings = await _context.ClinicSettings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(cs => cs.TenantId == tenant.Id && !cs.IsDeleted);

        var dto = new PublicClinicDto
        {
            ClinicName = settings?.ClinicName ?? tenant.Name,
            Phone = settings?.Phone ?? tenant.ContactPhone,
            SupportWhatsAppNumber = settings?.SupportWhatsAppNumber,
            SupportPhoneNumber = settings?.SupportPhoneNumber,
            Address = settings?.Address ?? tenant.Address,
            City = settings?.City,
            LogoUrl = settings?.LogoUrl ?? tenant.LogoUrl,
            BookingEnabled = settings?.BookingEnabled ?? false,
            TenantSlug = tenant.Slug,
            IsActive = tenant.Status == TenantStatus.Active
        };

        return ApiResponse<PublicClinicDto>.Ok(dto, "Clinic profile retrieved");
    }

    public async Task<ApiResponse<List<PublicDoctorDto>>> GetDoctorsAsync(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);

        if (tenant == null)
            return ApiResponse<List<PublicDoctorDto>>.Ok(new List<PublicDoctorDto>(), "Clinic not found");

        var doctors = await _context.Doctors.IgnoreQueryFilters()
            .Include(d => d.Services)
            .Where(d => d.TenantId == tenant.Id && !d.IsDeleted && d.IsEnabled)
            .ToListAsync();

        var dtos = doctors.Select(d => new PublicDoctorDto
        {
            Id = d.Id,
            Name = d.Name,
            Specialty = d.Specialty,
            Bio = d.Bio,
            PhotoUrl = d.PhotoUrl,
            IsEnabled = d.IsEnabled,
            AvgVisitDurationMinutes = d.AvgVisitDurationMinutes,
            Services = d.Services.Where(s => s.IsActive && !s.IsDeleted).Select(s => new PublicDoctorServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes
            }).ToList()
        }).ToList();

        return ApiResponse<List<PublicDoctorDto>>.Ok(dtos, $"Retrieved {dtos.Count} doctor(s)");
    }

    public async Task<ApiResponse<List<PublicDoctorServiceDto>>> GetServicesAsync(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);

        if (tenant == null)
            return ApiResponse<List<PublicDoctorServiceDto>>.Ok(new List<PublicDoctorServiceDto>(), "Clinic not found");

        var services = await _context.DoctorServices.IgnoreQueryFilters()
            .Where(ds => ds.TenantId == tenant.Id && !ds.IsDeleted && ds.IsActive)
            .ToListAsync();

        var dtos = services.Select(s => new PublicDoctorServiceDto
        {
            Id = s.Id,
            ServiceName = s.ServiceName,
            Price = s.Price,
            DurationMinutes = s.DurationMinutes
        }).ToList();

        return ApiResponse<List<PublicDoctorServiceDto>>.Ok(dtos, $"Retrieved {dtos.Count} service(s)");
    }

    public async Task<ApiResponse<List<PublicWorkingHourDto>>> GetWorkingHoursAsync(string tenantSlug)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);

        if (tenant == null)
            return ApiResponse<List<PublicWorkingHourDto>>.Ok(new List<PublicWorkingHourDto>(), "Clinic not found");

        var settings = await _context.ClinicSettings.IgnoreQueryFilters()
            .Include(cs => cs.WorkingHours)
            .FirstOrDefaultAsync(cs => cs.TenantId == tenant.Id && !cs.IsDeleted);

        if (settings == null)
            return ApiResponse<List<PublicWorkingHourDto>>.Ok(new List<PublicWorkingHourDto>(), "No working hours configured");

        var dtos = settings.WorkingHours.Where(w => !w.IsDeleted).Select(w => new PublicWorkingHourDto
        {
            DayOfWeek = w.DayOfWeek.ToString(),
            StartTime = w.StartTime.ToString(@"hh\:mm"),
            EndTime = w.EndTime.ToString(@"hh\:mm"),
            IsActive = w.IsActive
        }).ToList();

        return ApiResponse<List<PublicWorkingHourDto>>.Ok(dtos, $"Retrieved {dtos.Count} working hour(s)");
    }
}
