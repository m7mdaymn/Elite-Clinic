using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EliteClinic.Application.Features.Clinic.Services;

public class PatientService : IPatientService
{
    private readonly EliteClinicDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientService(EliteClinicDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApiResponse<CreatePatientResponse>> CreatePatientAsync(Guid tenantId, CreatePatientRequest request)
    {
        // Resolve tenant slug for username generation
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (tenant == null)
            return ApiResponse<CreatePatientResponse>.Error("Tenant not found");

        // Generate unique username: patient_{tenantSlug}_{sequence}
        var patientCount = await _context.Patients
            .Where(p => p.TenantId == tenantId)
            .CountAsync();
        var seq = patientCount + 1;
        var username = $"patient_{tenant.Slug}_{seq}";

        // Ensure uniqueness
        while (await _userManager.FindByNameAsync(username) != null)
        {
            seq++;
            username = $"patient_{tenant.Slug}_{seq}";
        }

        // Generate random password
        var password = GeneratePassword();

        // Create ApplicationUser
        var user = new ApplicationUser(username, request.Name)
        {
            TenantId = tenantId,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => (object)new { field = "User", message = e.Description }).ToList();
            return ApiResponse<CreatePatientResponse>.ValidationError(errors, "Failed to create patient account");
        }

        await _userManager.AddToRoleAsync(user, "Patient");

        // Create Patient entity
        var patient = new Patient
        {
            TenantId = tenantId,
            UserId = user.Id,
            Name = request.Name,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            Notes = request.Notes,
            IsDefault = true,
            ParentPatientId = null
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var dto = MapToDto(patient, user);

        return ApiResponse<CreatePatientResponse>.Created(new CreatePatientResponse
        {
            Patient = dto,
            Username = username,
            Password = password
        }, "Patient created successfully");
    }

    public async Task<ApiResponse<PagedResult<PatientDto>>> GetAllPatientsAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .Where(p => p.TenantId == tenantId && p.ParentPatientId == null) // Only root patients
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Phone.Contains(s));
        }

        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = patients.Select(p => MapToDto(p, p.User)).ToList();

        var result = new PagedResult<PatientDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<PatientDto>>.Ok(result, $"Retrieved {dtos.Count} patient(s)");
    }

    public async Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid tenantId, Guid id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (patient == null)
            return ApiResponse<PatientDto>.Error("Patient not found");

        return ApiResponse<PatientDto>.Ok(MapToDto(patient, patient.User), "Patient retrieved successfully");
    }

    public async Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid tenantId, Guid id, UpdatePatientRequest request)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (patient == null)
            return ApiResponse<PatientDto>.Error("Patient not found");

        patient.Name = request.Name;
        patient.Phone = request.Phone;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Address = request.Address;
        patient.Notes = request.Notes;

        patient.User.DisplayName = request.Name;
        await _userManager.UpdateAsync(patient.User);

        await _context.SaveChangesAsync();

        return ApiResponse<PatientDto>.Ok(MapToDto(patient, patient.User), "Patient updated successfully");
    }

    public async Task<ApiResponse<PatientDto>> AddSubProfileAsync(Guid tenantId, Guid parentId, AddSubProfileRequest request)
    {
        var parent = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == parentId && p.TenantId == tenantId && p.ParentPatientId == null);

        if (parent == null)
            return ApiResponse<PatientDto>.Error("Parent patient not found");

        // Sub-profiles share parent's ApplicationUser login
        var subProfile = new Patient
        {
            TenantId = tenantId,
            UserId = parent.UserId, // Same user account
            Name = request.Name,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            IsDefault = false,
            ParentPatientId = parentId
        };

        _context.Patients.Add(subProfile);
        await _context.SaveChangesAsync();

        // Reload parent with sub-profiles
        var updated = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .FirstAsync(p => p.Id == parentId);

        return ApiResponse<PatientDto>.Created(MapToDto(updated, updated.User), "Sub-profile added successfully");
    }

    public async Task<ApiResponse<ResetPasswordResponse>> ResetPasswordAsync(Guid tenantId, Guid id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.ParentPatientId == null);

        if (patient == null)
            return ApiResponse<ResetPasswordResponse>.Error("Patient not found");

        var newPassword = GeneratePassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(patient.User);
        var resetResult = await _userManager.ResetPasswordAsync(patient.User, token, newPassword);

        if (!resetResult.Succeeded)
        {
            return ApiResponse<ResetPasswordResponse>.Error("Failed to reset password");
        }

        return ApiResponse<ResetPasswordResponse>.Ok(new ResetPasswordResponse
        {
            NewPassword = newPassword
        }, "Password reset successfully");
    }

    public async Task<ApiResponse<object>> DeletePatientAsync(Guid tenantId, Guid id)
    {
        var patient = await _context.Patients
            .Include(p => p.SubProfiles.Where(sp => !sp.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.ParentPatientId == null);

        if (patient == null)
            return ApiResponse<object>.Error("Patient not found");

        // Soft-delete all sub-profiles
        foreach (var sub in patient.SubProfiles)
        {
            sub.IsDeleted = true;
            sub.DeletedAt = DateTime.UtcNow;
        }

        // Soft-delete the patient (BaseEntity.IsDeleted handled by SaveChangesAsync override)
        _context.Patients.Remove(patient); // Will be intercepted and soft-deleted

        await _context.SaveChangesAsync();

        return ApiResponse<object>.Ok(null!, "Patient deleted successfully");
    }

    private static string GeneratePassword()
    {
        var random = new Random();
        var rng = random.Next(1000, 9999);
        return $"Patient@{rng}";
    }

    private static PatientDto MapToDto(Patient patient, ApplicationUser user)
    {
        return new PatientDto
        {
            Id = patient.Id,
            UserId = patient.UserId,
            Name = patient.Name,
            Phone = patient.Phone,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            Notes = patient.Notes,
            IsDefault = patient.IsDefault,
            ParentPatientId = patient.ParentPatientId,
            Username = user.UserName ?? string.Empty,
            SubProfiles = patient.SubProfiles?
                .Where(sp => !sp.IsDeleted)
                .Select(sp => new PatientSubProfileDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Phone = sp.Phone,
                    DateOfBirth = sp.DateOfBirth,
                    Gender = sp.Gender,
                    IsDefault = sp.IsDefault
                }).ToList() ?? new(),
            CreatedAt = patient.CreatedAt
        };
    }
}
