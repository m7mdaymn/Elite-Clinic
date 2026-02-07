using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

/// <summary>
/// Public SEO endpoints. No auth required. Always returns 200.
/// Tenant identified by slug in the URL path.
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IPublicService _publicService;

    public PublicController(IPublicService publicService)
    {
        _publicService = publicService;
    }

    /// <summary>
    /// Get public clinic profile (no auth, always 200)
    /// </summary>
    [HttpGet("{slug}/clinic")]
    [ProducesResponseType(typeof(ApiResponse<PublicClinicDto>), 200)]
    public async Task<ActionResult<ApiResponse<PublicClinicDto>>> GetClinicProfile(string slug)
    {
        var result = await _publicService.GetClinicProfileAsync(slug);
        return Ok(result);
    }

    /// <summary>
    /// Get public list of enabled doctors (no auth, always 200)
    /// </summary>
    [HttpGet("{slug}/doctors")]
    [ProducesResponseType(typeof(ApiResponse<List<PublicDoctorDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<PublicDoctorDto>>>> GetDoctors(string slug)
    {
        var result = await _publicService.GetDoctorsAsync(slug);
        return Ok(result);
    }

    /// <summary>
    /// Get public list of active services (no auth, always 200)
    /// </summary>
    [HttpGet("{slug}/services")]
    [ProducesResponseType(typeof(ApiResponse<List<PublicDoctorServiceDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<PublicDoctorServiceDto>>>> GetServices(string slug)
    {
        var result = await _publicService.GetServicesAsync(slug);
        return Ok(result);
    }

    /// <summary>
    /// Get public working hours (no auth, always 200)
    /// </summary>
    [HttpGet("{slug}/working-hours")]
    [ProducesResponseType(typeof(ApiResponse<List<PublicWorkingHourDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<PublicWorkingHourDto>>>> GetWorkingHours(string slug)
    {
        var result = await _publicService.GetWorkingHoursAsync(slug);
        return Ok(result);
    }
}
