using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class StaffService : IStaffService
{
    private readonly EliteClinicDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffService(EliteClinicDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ApiResponse<StaffDto>> CreateStaffAsync(Guid tenantId, CreateStaffRequest request)
    {
        // Check username uniqueness
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            return ApiResponse<StaffDto>.ValidationError(
                new List<object> { new { field = "Username", message = "Username already taken" } });
        }

        // Create ApplicationUser
        var user = new ApplicationUser(request.Username, request.Name)
        {
            TenantId = tenantId,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => (object)new { field = "Password", message = e.Description }).ToList();
            return ApiResponse<StaffDto>.ValidationError(errors, "Failed to create user account");
        }

        // Determine role (default to ClinicManager)
        var validRoles = new[] { "ClinicManager", "Receptionist" };
        var role = !string.IsNullOrWhiteSpace(request.Role) && validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase)
            ? request.Role
            : "ClinicManager";

        await _userManager.AddToRoleAsync(user, role);

        // Create Employee entity
        var employee = new Employee
        {
            TenantId = tenantId,
            UserId = user.Id,
            Name = request.Name,
            Phone = request.Phone,
            Role = role,
            Salary = request.Salary,
            HireDate = request.HireDate,
            Notes = request.Notes,
            IsEnabled = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return ApiResponse<StaffDto>.Created(MapToDto(employee, user), "Staff member created successfully");
    }

    public async Task<ApiResponse<PagedResult<StaffDto>>> GetAllStaffAsync(Guid tenantId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Employees
            .Include(e => e.User)
            .Where(e => e.TenantId == tenantId)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var staff = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = staff.Select(e => MapToDto(e, e.User)).ToList();

        var result = new PagedResult<StaffDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<StaffDto>>.Ok(result, $"Retrieved {dtos.Count} staff member(s)");
    }

    public async Task<ApiResponse<StaffDto>> GetStaffByIdAsync(Guid tenantId, Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (employee == null)
            return ApiResponse<StaffDto>.Error("Staff member not found");

        return ApiResponse<StaffDto>.Ok(MapToDto(employee, employee.User), "Staff member retrieved successfully");
    }

    public async Task<ApiResponse<StaffDto>> UpdateStaffAsync(Guid tenantId, Guid id, UpdateStaffRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (employee == null)
            return ApiResponse<StaffDto>.Error("Staff member not found");

        employee.Name = request.Name;
        employee.Phone = request.Phone;
        employee.Salary = request.Salary;
        employee.HireDate = request.HireDate;
        employee.Notes = request.Notes;

        // Update ApplicationUser display name
        employee.User.DisplayName = request.Name;
        await _userManager.UpdateAsync(employee.User);

        await _context.SaveChangesAsync();

        return ApiResponse<StaffDto>.Ok(MapToDto(employee, employee.User), "Staff member updated successfully");
    }

    public async Task<ApiResponse<StaffDto>> EnableStaffAsync(Guid tenantId, Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (employee == null)
            return ApiResponse<StaffDto>.Error("Staff member not found");

        employee.IsEnabled = true;
        employee.User.IsActive = true;
        await _userManager.UpdateAsync(employee.User);
        await _context.SaveChangesAsync();

        return ApiResponse<StaffDto>.Ok(MapToDto(employee, employee.User), "Staff member enabled successfully");
    }

    public async Task<ApiResponse<StaffDto>> DisableStaffAsync(Guid tenantId, Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (employee == null)
            return ApiResponse<StaffDto>.Error("Staff member not found");

        employee.IsEnabled = false;
        employee.User.IsActive = false;
        await _userManager.UpdateAsync(employee.User);
        await _context.SaveChangesAsync();

        return ApiResponse<StaffDto>.Ok(MapToDto(employee, employee.User), "Staff member disabled successfully");
    }

    private static StaffDto MapToDto(Employee employee, ApplicationUser user)
    {
        return new StaffDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            Name = employee.Name,
            Phone = employee.Phone,
            Role = employee.Role,
            Username = user.UserName ?? string.Empty,
            Salary = employee.Salary,
            HireDate = employee.HireDate,
            Notes = employee.Notes,
            IsEnabled = employee.IsEnabled,
            CreatedAt = employee.CreatedAt
        };
    }
}
