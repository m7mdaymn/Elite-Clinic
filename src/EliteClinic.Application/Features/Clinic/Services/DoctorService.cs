using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class DoctorServiceImpl : IDoctorService
{
    private readonly EliteClinicDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorServiceImpl(EliteClinicDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApiResponse<DoctorDto>> CreateDoctorAsync(Guid tenantId, CreateDoctorRequest request)
    {
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return ApiResponse<DoctorDto>.ValidationError(
                new List<object> { new { field = "Username", message = "Username already taken" } });
        }

        var user = new ApplicationUser(request.Username, request.Name)
        {
            TenantId = tenantId,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => (object)new { field = "Password", message = e.Description }).ToList();
            return ApiResponse<DoctorDto>.ValidationError(errors, "Failed to create user account");
        }

        await _userManager.AddToRoleAsync(user, "Doctor");

        var doctor = new Doctor
        {
            TenantId = tenantId,
            UserId = user.Id,
            Name = request.Name,
            Specialty = request.Specialty,
            Phone = request.Phone,
            Bio = request.Bio,
            PhotoUrl = request.PhotoUrl,
            UrgentCaseMode = request.UrgentCaseMode,
            AvgVisitDurationMinutes = request.AvgVisitDurationMinutes,
            IsEnabled = true
        };

        _context.Doctors.Add(doctor);

        // Auto-create default visit field config
        var visitConfig = new DoctorVisitFieldConfig
        {
            TenantId = tenantId,
            DoctorId = doctor.Id,
            Temperature = true,
            Weight = true
        };
        _context.DoctorVisitFieldConfigs.Add(visitConfig);

        await _context.SaveChangesAsync();

        var saved = await GetDoctorWithIncludes(tenantId, doctor.Id);
        return ApiResponse<DoctorDto>.Created(MapToDto(saved!, saved!.User), "Doctor created successfully");
    }

    public async Task<ApiResponse<PagedResult<DoctorDto>>> GetAllDoctorsAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Services.Where(s => !s.IsDeleted))
            .Include(d => d.VisitFieldConfig)
            .Where(d => d.TenantId == tenantId)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var doctors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = doctors.Select(d => MapToDto(d, d.User)).ToList();

        var result = new PagedResult<DoctorDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<DoctorDto>>.Ok(result, $"Retrieved {dtos.Count} doctor(s)");
    }

    public async Task<ApiResponse<DoctorDto>> GetDoctorByIdAsync(Guid tenantId, Guid id)
    {
        var doctor = await GetDoctorWithIncludes(tenantId, id);
        if (doctor == null)
            return ApiResponse<DoctorDto>.Error("Doctor not found");

        return ApiResponse<DoctorDto>.Ok(MapToDto(doctor, doctor.User), "Doctor retrieved successfully");
    }

    public async Task<ApiResponse<DoctorDto>> UpdateDoctorAsync(Guid tenantId, Guid id, UpdateDoctorRequest request)
    {
        var doctor = await GetDoctorWithIncludes(tenantId, id);
        if (doctor == null)
            return ApiResponse<DoctorDto>.Error("Doctor not found");

        doctor.Name = request.Name;
        doctor.Specialty = request.Specialty;
        doctor.Phone = request.Phone;
        doctor.Bio = request.Bio;
        doctor.PhotoUrl = request.PhotoUrl;
        doctor.UrgentCaseMode = request.UrgentCaseMode;
        doctor.AvgVisitDurationMinutes = request.AvgVisitDurationMinutes;

        doctor.User.DisplayName = request.Name;
        await _userManager.UpdateAsync(doctor.User);

        await _context.SaveChangesAsync();

        return ApiResponse<DoctorDto>.Ok(MapToDto(doctor, doctor.User), "Doctor updated successfully");
    }

    public async Task<ApiResponse<DoctorDto>> EnableDoctorAsync(Guid tenantId, Guid id)
    {
        var doctor = await GetDoctorWithIncludes(tenantId, id);
        if (doctor == null)
            return ApiResponse<DoctorDto>.Error("Doctor not found");

        doctor.IsEnabled = true;
        doctor.User.IsActive = true;
        await _userManager.UpdateAsync(doctor.User);
        await _context.SaveChangesAsync();

        return ApiResponse<DoctorDto>.Ok(MapToDto(doctor, doctor.User), "Doctor enabled successfully");
    }

    public async Task<ApiResponse<DoctorDto>> DisableDoctorAsync(Guid tenantId, Guid id)
    {
        var doctor = await GetDoctorWithIncludes(tenantId, id);
        if (doctor == null)
            return ApiResponse<DoctorDto>.Error("Doctor not found");

        doctor.IsEnabled = false;
        doctor.User.IsActive = false;
        await _userManager.UpdateAsync(doctor.User);
        await _context.SaveChangesAsync();

        return ApiResponse<DoctorDto>.Ok(MapToDto(doctor, doctor.User), "Doctor disabled successfully");
    }

    public async Task<ApiResponse<List<DoctorServiceDto>>> UpdateServicesAsync(Guid tenantId, Guid doctorId, UpdateDoctorServicesRequest request)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Services.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(d => d.Id == doctorId && d.TenantId == tenantId && !d.IsDeleted);

        if (doctor == null)
            return ApiResponse<List<DoctorServiceDto>>.Error("Doctor not found");

        // Soft-delete existing services
        foreach (var existing in doctor.Services)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
        }

        // Add new services — use _context.DoctorServices.Add() to force Added state
        // (BaseEntity sets Id = Guid.NewGuid() in constructor → EF treats as Modified if added to tracked collection)
        foreach (var svc in request.Services)
        {
            _context.DoctorServices.Add(new Domain.Entities.DoctorService
            {
                TenantId = tenantId,
                DoctorId = doctorId,
                ServiceName = svc.ServiceName,
                Price = svc.Price,
                DurationMinutes = svc.DurationMinutes,
                IsActive = svc.IsActive
            });
        }

        await _context.SaveChangesAsync();

        // Reload services from DB (new ones were added via _context.DoctorServices, not doctor.Services)
        var dtos = await _context.DoctorServices
            .Where(s => s.DoctorId == doctorId && !s.IsDeleted)
            .Select(s => new DoctorServiceDto
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            }).ToListAsync();

        return ApiResponse<List<DoctorServiceDto>>.Ok(dtos, "Doctor services updated successfully");
    }

    public async Task<ApiResponse<DoctorVisitFieldConfigDto>> UpdateVisitFieldsAsync(Guid tenantId, Guid doctorId, UpdateVisitFieldsRequest request)
    {
        var doctor = await _context.Doctors
            .Include(d => d.VisitFieldConfig)
            .FirstOrDefaultAsync(d => d.Id == doctorId && d.TenantId == tenantId && !d.IsDeleted);

        if (doctor == null)
            return ApiResponse<DoctorVisitFieldConfigDto>.Error("Doctor not found");

        if (doctor.VisitFieldConfig == null)
        {
            doctor.VisitFieldConfig = new DoctorVisitFieldConfig
            {
                TenantId = tenantId,
                DoctorId = doctorId
            };
            _context.DoctorVisitFieldConfigs.Add(doctor.VisitFieldConfig);
        }

        doctor.VisitFieldConfig.BloodPressure = request.BloodPressure;
        doctor.VisitFieldConfig.HeartRate = request.HeartRate;
        doctor.VisitFieldConfig.Temperature = request.Temperature;
        doctor.VisitFieldConfig.Weight = request.Weight;
        doctor.VisitFieldConfig.Height = request.Height;
        doctor.VisitFieldConfig.BMI = request.BMI;
        doctor.VisitFieldConfig.BloodSugar = request.BloodSugar;
        doctor.VisitFieldConfig.OxygenSaturation = request.OxygenSaturation;
        doctor.VisitFieldConfig.RespiratoryRate = request.RespiratoryRate;

        await _context.SaveChangesAsync();

        return ApiResponse<DoctorVisitFieldConfigDto>.Ok(new DoctorVisitFieldConfigDto
        {
            BloodPressure = doctor.VisitFieldConfig.BloodPressure,
            HeartRate = doctor.VisitFieldConfig.HeartRate,
            Temperature = doctor.VisitFieldConfig.Temperature,
            Weight = doctor.VisitFieldConfig.Weight,
            Height = doctor.VisitFieldConfig.Height,
            BMI = doctor.VisitFieldConfig.BMI,
            BloodSugar = doctor.VisitFieldConfig.BloodSugar,
            OxygenSaturation = doctor.VisitFieldConfig.OxygenSaturation,
            RespiratoryRate = doctor.VisitFieldConfig.RespiratoryRate
        }, "Visit field config updated successfully");
    }

    private async Task<Doctor?> GetDoctorWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Services.Where(s => !s.IsDeleted))
            .Include(d => d.VisitFieldConfig)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && !d.IsDeleted);
    }

    private static DoctorDto MapToDto(Doctor doctor, ApplicationUser user)
    {
        return new DoctorDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            Name = doctor.Name,
            Specialty = doctor.Specialty,
            Phone = doctor.Phone,
            Bio = doctor.Bio,
            PhotoUrl = doctor.PhotoUrl,
            IsEnabled = doctor.IsEnabled,
            Username = user.UserName ?? string.Empty,
            UrgentCaseMode = doctor.UrgentCaseMode,
            AvgVisitDurationMinutes = doctor.AvgVisitDurationMinutes,
            Services = doctor.Services
                .Where(s => !s.IsDeleted)
                .Select(s => new DoctorServiceDto
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    IsActive = s.IsActive
                }).ToList(),
            VisitFieldConfig = doctor.VisitFieldConfig != null ? new DoctorVisitFieldConfigDto
            {
                BloodPressure = doctor.VisitFieldConfig.BloodPressure,
                HeartRate = doctor.VisitFieldConfig.HeartRate,
                Temperature = doctor.VisitFieldConfig.Temperature,
                Weight = doctor.VisitFieldConfig.Weight,
                Height = doctor.VisitFieldConfig.Height,
                BMI = doctor.VisitFieldConfig.BMI,
                BloodSugar = doctor.VisitFieldConfig.BloodSugar,
                OxygenSaturation = doctor.VisitFieldConfig.OxygenSaturation,
                RespiratoryRate = doctor.VisitFieldConfig.RespiratoryRate
            } : null,
            CreatedAt = doctor.CreatedAt
        };
    }
}
