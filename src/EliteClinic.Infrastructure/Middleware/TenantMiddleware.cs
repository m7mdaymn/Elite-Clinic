using EliteClinic.Domain.Enums;
using EliteClinic.Infrastructure.Data;
using EliteClinic.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EliteClinic.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, EliteClinicDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip tenant resolution for these routes
        if (IsPublicRoute(path))
        {
            await _next(context);
            return;
        }

        var tenantHeader = context.Request.Headers["X-Tenant"].ToString();

        if (RequiresTenant(path))
        {
            if (string.IsNullOrWhiteSpace(tenantHeader))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var errorResponse = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "X-Tenant header is required",
                    errors = new[] { new { field = "X-Tenant", message = "Header is missing" } }
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Try to resolve tenant (synchronous query, no tracking to avoid concurrency issues)
            var tenant = dbContext.Tenants.AsNoTracking().FirstOrDefault(t => t.Slug == tenantHeader && !t.IsDeleted);

            if (tenant == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                var errorResponse = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Tenant not found",
                    errors = new object[] { }
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Check tenant status (Inactive, Suspended, Blocked)
            if (tenant.Status != TenantStatus.Active)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                
                var message = tenant.Status switch
                {
                    TenantStatus.Inactive => "Tenant is inactive. Contact platform support.",
                    TenantStatus.Suspended => "Tenant is suspended. Contact platform support.",
                    TenantStatus.Blocked => "Tenant is blocked. Contact platform support.",
                    _ => "Tenant access is restricted. Contact platform support."
                };

                var errorResponse = JsonSerializer.Serialize(new
                {
                    success = false,
                    message,
                    errors = new object[] { }
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Cross-tenant access guard: Validate JWT tenantId matches X-Tenant
            // SuperAdmin (tenantId claim = null or missing) bypasses this check
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jwtTenantIdClaim = context.User.FindFirst("tenantId")?.Value;
                
                // If user has tenantId claim (not SuperAdmin), validate it matches X-Tenant header
                if (!string.IsNullOrEmpty(jwtTenantIdClaim))
                {
                    if (Guid.TryParse(jwtTenantIdClaim, out var jwtTenantId))
                    {
                        if (jwtTenantId != tenant.Id)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";
                            var errorResponse = JsonSerializer.Serialize(new
                            {
                                success = false,
                                message = "Access denied. You cannot access resources from another tenant.",
                                errors = new object[] { }
                            });
                            await context.Response.WriteAsync(errorResponse);
                            return;
                        }
                    }
                }
                // If jwtTenantIdClaim is null or empty, user is SuperAdmin - allow access to any tenant
            }

            // Set tenant context
            var tc = (TenantContext)tenantContext;
            tc.TenantId = tenant.Id;
            tc.TenantSlug = tenant.Slug;
            tc.TenantStatus = tenant.Status;
            tc.IsTenantResolved = true;
        }

        await _next(context);
    }

    private bool IsPublicRoute(string path)
    {
        // These routes do not require tenant resolution
        var publicPaths = new[]
        {
            "/api/health",
            "/api/public",
            "/swagger",
            "/api-docs"
        };

        return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool RequiresTenant(string path)
    {
        // These routes require tenant header (all tenant-scoped routes)
        // Exclude platform routes and public routes
        var isPlatformRoute = path.StartsWith("/api/platform", StringComparison.OrdinalIgnoreCase) ||
                              path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) ||
                              path.StartsWith("/api/public", StringComparison.OrdinalIgnoreCase) ||
                              path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                              path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase);

        return !isPlatformRoute;
    }
}
