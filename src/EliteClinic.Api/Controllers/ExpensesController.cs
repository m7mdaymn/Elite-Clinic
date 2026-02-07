using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ITenantContext _tenantContext;

    public ExpensesController(IExpenseService expenseService, ITenantContext tenantContext)
    {
        _expenseService = expenseService;
        _tenantContext = tenantContext;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Add expense
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Create([FromBody] CreateExpenseRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ExpenseDto>.Error("Tenant context not resolved"));

        var result = await _expenseService.CreateAsync(_tenantContext.TenantId, request, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Update expense
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Update(Guid id, [FromBody] UpdateExpenseRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<ExpenseDto>.Error("Tenant context not resolved"));

        var result = await _expenseService.UpdateAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Delete expense (ClinicOwner only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse.Error("Tenant context not resolved"));

        var result = await _expenseService.DeleteAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// List expenses (paginated, filterable by date & category)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ExpenseDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseDto>>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? category,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<ExpenseDto>>.Error("Tenant context not resolved"));

        var result = await _expenseService.GetAllAsync(_tenantContext.TenantId, from, to, category, pageNumber, pageSize);
        return Ok(result);
    }
}
