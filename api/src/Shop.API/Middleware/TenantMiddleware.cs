using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Services;

namespace Shop.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ShopDbContext db, TenantContext tenantContext)
    {
        // Skip tenant resolution for platform-level endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/api/platform/") || path.StartsWith("/api/health") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var tenant = await ResolveTenantAsync(context, db);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for request: {Path}", context.Request.Path);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "테넌트를 찾을 수 없습니다." });
            return;
        }

        if (!tenant.IsActive)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "비활성화된 테넌트입니다." });
            return;
        }

        tenantContext.SetTenant(tenant);
        _logger.LogDebug("Tenant resolved: {Slug} (ID: {Id})", tenant.Slug, tenant.Id);

        await _next(context);
    }

    private static async Task<Tenant?> ResolveTenantAsync(HttpContext context, ShopDbContext db)
    {
        // 1. X-Tenant-Id header (dev/test)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            var slug = tenantHeader.ToString();
            return await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        }

        // 2. Custom domain matching
        var host = context.Request.Host.Host.ToLower();

        var tenantByDomain = await db.Tenants
            .FirstOrDefaultAsync(t => t.CustomDomain != null && t.CustomDomain.ToLower() == host);
        if (tenantByDomain != null) return tenantByDomain;

        // 3. Subdomain matching (e.g., catholia.syndock.shop)
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            var subdomain = parts[0];
            var tenantBySub = await db.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain != null && t.Subdomain.ToLower() == subdomain);
            if (tenantBySub != null) return tenantBySub;
        }

        return null;
    }
}
