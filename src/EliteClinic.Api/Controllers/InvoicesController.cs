using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Application.Features.Clinic.Services;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteClinic.Api.Controllers;

[ApiController]
[Route("api/clinic/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ITenantContext _tenantContext;

    public InvoicesController(IInvoiceService invoiceService, ITenantContext tenantContext)
    {
        _invoiceService = invoiceService;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Create an invoice for a visit
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<InvoiceDto>.Error("Tenant context not resolved"));

        var result = await _invoiceService.CreateInvoiceAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Update invoice (only while visit is Open)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<InvoiceDto>.Error("Tenant context not resolved"));

        var result = await _invoiceService.UpdateInvoiceAsync(_tenantContext.TenantId, id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get invoice by ID with payments
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<InvoiceDto>.Error("Tenant context not resolved"));

        var result = await _invoiceService.GetInvoiceByIdAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// List invoices (paginated, filterable by date & doctor)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceDto>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceDto>>>> GetInvoices(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? doctorId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PagedResult<InvoiceDto>>.Error("Tenant context not resolved"));

        var result = await _invoiceService.GetInvoicesAsync(_tenantContext.TenantId, from, to, doctorId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Record a payment against an invoice (partial payments allowed)
    /// </summary>
    [HttpPost("~/api/clinic/payments")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> RecordPayment([FromBody] CreatePaymentRequest request)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<PaymentDto>.Error("Tenant context not resolved"));

        var result = await _invoiceService.RecordPaymentAsync(_tenantContext.TenantId, request);
        if (!result.Success)
            return BadRequest(result);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Get all payments for an invoice
    /// </summary>
    [HttpGet("{id:guid}/payments")]
    [Authorize(Roles = "ClinicOwner,ClinicManager,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<PaymentDto>>>> GetPayments(Guid id)
    {
        if (!_tenantContext.IsTenantResolved)
            return BadRequest(ApiResponse<List<PaymentDto>>.Error("Tenant context not resolved"));

        var result = await _invoiceService.GetPaymentsByInvoiceAsync(_tenantContext.TenantId, id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
