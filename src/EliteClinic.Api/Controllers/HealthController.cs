using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <param name="dbContext">Database context for connectivity check</param>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthDto>), 200)]
    public async Task<ActionResult<ApiResponse<HealthDto>>> GetHealth(
        [FromServices] EliteClinic.Infrastructure.Data.EliteClinicDbContext dbContext)
    {
        var databaseStatus = "Error";
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            databaseStatus = canConnect ? "Connected" : "Error";
        }
        catch
        {
            databaseStatus = "Error";
        }

        var response = ApiResponse<HealthDto>.Ok(new HealthDto
        {
            Status = "Healthy",
            Database = databaseStatus,
            Version = "0.0.1",
            Timestamp = DateTime.UtcNow
        });

        return Ok(response);
    }
}
