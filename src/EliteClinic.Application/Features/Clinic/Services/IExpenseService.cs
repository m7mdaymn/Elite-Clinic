using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;

namespace EliteClinic.Application.Features.Clinic.Services;

public interface IExpenseService
{
    Task<ApiResponse<ExpenseDto>> CreateAsync(Guid tenantId, CreateExpenseRequest request, Guid callerUserId);
    Task<ApiResponse<ExpenseDto>> UpdateAsync(Guid tenantId, Guid expenseId, UpdateExpenseRequest request);
    Task<ApiResponse> DeleteAsync(Guid tenantId, Guid expenseId);
    Task<ApiResponse<PagedResult<ExpenseDto>>> GetAllAsync(Guid tenantId, DateTime? from, DateTime? to, string? category, int pageNumber = 1, int pageSize = 10);
}
