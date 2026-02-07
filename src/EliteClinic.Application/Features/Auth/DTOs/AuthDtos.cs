namespace EliteClinic.Application.Features.Auth.DTOs;

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; }
}

public class PatientLoginResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public PatientUserInfoDto User { get; set; }
}

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class PatientUserInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }
    public Guid? TenantId { get; set; }
    public List<PatientProfileDto> Profiles { get; set; } = new();
}

public class PatientProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsDefault { get; set; }
}

public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public class HealthDto
{
    public string Status { get; set; }
    public string Database { get; set; }
    public string Version { get; set; }
    public DateTime Timestamp { get; set; }
}
