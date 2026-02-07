using EliteClinic.Application.Features.Auth.DTOs;
using EliteClinic.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using EliteClinic.Infrastructure.Data;

namespace EliteClinic.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password, string? tenantSlug = null);
    Task<PatientLoginResponse?> PatientLoginAsync(string username, string password, string tenantSlug);
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
    Task<UserInfoDto?> GetCurrentUserAsync(ClaimsPrincipal claims, string? tenantSlug = null);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly EliteClinicDbContext _dbContext;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        EliteClinicDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password, string? tenantSlug = null)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            return null;

        if (!user.IsActive)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Unknown";

        // Tenant-scoped validation for non-SuperAdmin users
        if (role != "SuperAdmin" && !string.IsNullOrWhiteSpace(tenantSlug))
        {
            var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);
            if (tenant == null) return null;

            // Validate user belongs to this tenant
            if (!user.TenantId.HasValue || user.TenantId.Value != tenant.Id)
                return null;
        }

        var token = GenerateJwtToken(user, roles, tokenExpiry: 8 * 60); // 8 hours for staff
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.UserName,
                DisplayName = user.DisplayName,
                Role = roles.FirstOrDefault() ?? "Unknown",
                TenantId = user.TenantId,
                TenantSlug = tenantSlug,
                Permissions = GetPermissions(roles.FirstOrDefault())
            }
        };
    }

    public async Task<PatientLoginResponse?> PatientLoginAsync(string username, string password, string tenantSlug)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            return null;

        if (!user.IsActive)
            return null;

        // Validate tenant
        var tenant = await _dbContext.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);
        if (tenant == null) return null;

        // Validate user belongs to this tenant
        if (!user.TenantId.HasValue || user.TenantId.Value != tenant.Id)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles, tokenExpiry: 365 * 24 * 60); // 365 days for patient
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(730); // 2 years
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Load real patient profiles
        var profiles = await _dbContext.Patients.IgnoreQueryFilters()
            .Where(p => p.UserId == user.Id && p.TenantId == tenant.Id && !p.IsDeleted)
            .Select(p => new PatientProfileDto
            {
                Id = p.Id,
                Name = p.Name,
                IsDefault = p.IsDefault
            })
            .ToListAsync();

        // Fallback if no Patient entity exists yet
        if (!profiles.Any())
        {
            profiles.Add(new PatientProfileDto
            {
                Id = user.Id,
                Name = user.DisplayName,
                IsDefault = true
            });
        }

        return new PatientLoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(365),
            User = new PatientUserInfoDto
            {
                Id = user.Id,
                Username = user.UserName,
                DisplayName = user.DisplayName,
                Role = roles.FirstOrDefault() ?? "Unknown",
                TenantId = user.TenantId,
                Profiles = profiles
            }
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var newToken = GenerateJwtToken(user, roles, tokenExpiry: 8 * 60);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.UserName,
                DisplayName = user.DisplayName,
                Role = roles.FirstOrDefault() ?? "Unknown",
                TenantId = user.TenantId,
                Permissions = GetPermissions(roles.FirstOrDefault())
            }
        };
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(ClaimsPrincipal claims, string? tenantSlug = null)
    {
        var userIdClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.UserName,
            DisplayName = user.DisplayName,
            Role = roles.FirstOrDefault() ?? "Unknown",
            TenantId = user.TenantId,
            TenantSlug = tenantSlug,
            Permissions = GetPermissions(roles.FirstOrDefault())
        };
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles, int tokenExpiry)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:SecretKey"] ?? "your-secret-key-that-is-very-long-and-secure"));

        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("displayName", user.DisplayName),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim("tenantId", user.TenantId.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "EliteClinic",
            audience: _configuration["JwtSettings:Audience"] ?? "EliteClinicUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenExpiry),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private List<string> GetPermissions(string? role)
    {
        return role switch
        {
            "SuperAdmin" => new List<string>
            {
                "platform.view", "tenant.create", "tenant.manage", "subscription.manage",
                "feature_flags.manage", "audit.view", "analytics.view"
            },
            "ClinicOwner" => new List<string>
            {
                "clinic.manage", "doctor.manage", "staff.manage", "patient.view",
                "queue.manage", "visit.view", "finance.view", "report.export"
            },
            "ClinicManager" => new List<string>
            {
                "queue.manage", "patient.register", "payment.record", "expense.add",
                "finance.today"
            },
            "Doctor" => new List<string>
            {
                "queue.my", "visit.create", "prescription.create", "lab.request"
            },
            "Patient" => new List<string>
            {
                "queue.view", "booking.create", "profile.view"
            },
            _ => new List<string>()
        };
    }
}
