using EliteClinic.Application.Common.Models;
using EliteClinic.Application.Features.Clinic.DTOs;
using EliteClinic.Domain.Entities;
using EliteClinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EliteClinic.Application.Features.Clinic.Services;

public class ExpenseService : IExpenseService
{
    private readonly EliteClinicDbContext _context;

    public ExpenseService(EliteClinicDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ExpenseDto>> CreateAsync(Guid tenantId, CreateExpenseRequest request, Guid callerUserId)
    {
        if (request.Amount <= 0)
            return ApiResponse<ExpenseDto>.Error("Amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.Category))
            return ApiResponse<ExpenseDto>.Error("Category is required");

        var expense = new Expense
        {
            TenantId = tenantId,
            Category = request.Category,
            Amount = request.Amount,
            Notes = request.Notes,
            ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow.Date,
            RecordedByUserId = callerUserId
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        var saved = await GetExpenseWithIncludes(tenantId, expense.Id);
        return ApiResponse<ExpenseDto>.Created(MapToDto(saved!), "Expense recorded successfully");
    }

    public async Task<ApiResponse<ExpenseDto>> UpdateAsync(Guid tenantId, Guid expenseId, UpdateExpenseRequest request)
    {
        var expense = await GetExpenseWithIncludes(tenantId, expenseId);
        if (expense == null)
            return ApiResponse<ExpenseDto>.Error("Expense not found");

        if (request.Amount <= 0)
            return ApiResponse<ExpenseDto>.Error("Amount must be greater than zero");

        expense.Category = request.Category;
        expense.Amount = request.Amount;
        expense.Notes = request.Notes;
        if (request.ExpenseDate.HasValue)
            expense.ExpenseDate = request.ExpenseDate.Value;

        await _context.SaveChangesAsync();

        var updated = await GetExpenseWithIncludes(tenantId, expenseId);
        return ApiResponse<ExpenseDto>.Ok(MapToDto(updated!), "Expense updated successfully");
    }

    public async Task<ApiResponse> DeleteAsync(Guid tenantId, Guid expenseId)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.TenantId == tenantId && !e.IsDeleted);
        if (expense == null)
            return ApiResponse.Error("Expense not found");

        _context.Expenses.Remove(expense); // soft-delete via SaveChangesAsync override
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Expense deleted successfully");
    }

    public async Task<ApiResponse<PagedResult<ExpenseDto>>> GetAllAsync(Guid tenantId, DateTime? from, DateTime? to,
        string? category, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Expenses
            .Include(e => e.RecordedBy)
            .Where(e => e.TenantId == tenantId && !e.IsDeleted);

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);
        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        var totalCount = await query.CountAsync();
        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<ExpenseDto>
        {
            Items = expenses.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResult<ExpenseDto>>.Ok(result, $"Retrieved {result.Items.Count} expense(s)");
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<Expense?> GetExpenseWithIncludes(Guid tenantId, Guid id)
    {
        return await _context.Expenses
            .Include(e => e.RecordedBy)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId && !e.IsDeleted);
    }

    private static ExpenseDto MapToDto(Expense e)
    {
        return new ExpenseDto
        {
            Id = e.Id,
            Category = e.Category,
            Amount = e.Amount,
            Notes = e.Notes,
            ExpenseDate = e.ExpenseDate,
            RecordedByUserId = e.RecordedByUserId,
            RecordedByName = e.RecordedBy?.DisplayName ?? string.Empty,
            CreatedAt = e.CreatedAt
        };
    }
}
