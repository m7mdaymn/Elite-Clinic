using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Auth.DTOs;
using EliteClinic.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Staff/Doctor login
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="tenantSlug">Tenant slug from X-Tenant header (optional for SuperAdmin)</param>
    /// <returns>JWT token and user info</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        [FromHeader(Name = "X-Tenant")] string? tenantSlug = null)
    {
        if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            var errors = new List<object>();
            if (string.IsNullOrWhiteSpace(request?.Username))
                errors.Add(new { field = "username", message = "Username is required" });
            if (string.IsNullOrWhiteSpace(request?.Password))
                errors.Add(new { field = "password", message = "Password is required" });

            return BadRequest(ApiResponse<LoginResponse>.ValidationError(errors));
        }

        var result = await _authService.LoginAsync(request.Username, request.Password, tenantSlug);
        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(ApiResponse<LoginResponse>.Error("Invalid username or password"));
        }

        _logger.LogInformation("Successful login for user: {Username}", request.Username);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful"));
    }

    /// <summary>
    /// Patient login (persistent session)
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="tenantSlug">Tenant slug from X-Tenant header (required)</param>
    /// <returns>JWT token and patient info with profiles</returns>
    [HttpPost("patient/login")]
    [ProducesResponseType(typeof(ApiResponse<PatientLoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<PatientLoginResponse>>> PatientLogin(
        [FromBody] LoginRequest request,
        [FromHeader(Name = "X-Tenant")] string? tenantSlug = null)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return BadRequest(ApiResponse<PatientLoginResponse>.Error("X-Tenant header is required"));
        }

        if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            var errors = new List<object>();
            if (string.IsNullOrWhiteSpace(request?.Username))
                errors.Add(new { field = "username", message = "Username is required" });
            if (string.IsNullOrWhiteSpace(request?.Password))
                errors.Add(new { field = "password", message = "Password is required" });

            return BadRequest(ApiResponse<PatientLoginResponse>.ValidationError(errors));
        }

        var result = await _authService.PatientLoginAsync(request.Username, request.Password, tenantSlug);
        if (result == null)
        {
            _logger.LogWarning("Failed patient login attempt for user: {Username}", request.Username);
            return Unauthorized(ApiResponse<PatientLoginResponse>.Error("Invalid username or password"));
        }

        _logger.LogInformation("Successful patient login for user: {Username}", request.Username);
        return Ok(ApiResponse<PatientLoginResponse>.Ok(result, "Login successful"));
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="request">Refresh token from login response</param>
    /// <returns>New JWT token and refresh token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return BadRequest(ApiResponse<LoginResponse>.Error("Refresh token is required"));
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result == null)
        {
            _logger.LogWarning("Failed token refresh attempt with invalid token");
            return Unauthorized(ApiResponse<LoginResponse>.Error("Invalid or expired refresh token"));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed successfully"));
    }

    /// <summary>
    /// Get current authenticated user profile
    /// </summary>
    /// <param name="tenantSlug">Tenant slug from X-Tenant header (optional for SuperAdmin)</param>
    /// <returns>Current user info</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> GetMe(
        [FromHeader(Name = "X-Tenant")] string? tenantSlug = null)
    {
        var user = await _authService.GetCurrentUserAsync(User, tenantSlug);
        if (user == null)
        {
            return Unauthorized(ApiResponse<UserInfoDto>.Error("User not found"));
        }

        return Ok(ApiResponse<UserInfoDto>.Ok(user, "User retrieved successfully"));
    }
}
