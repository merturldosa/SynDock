using System.Text.Json;
using Shop.Infrastructure.Services;

namespace Shop.API.Middleware;

public class FeatureGatingMiddleware
{
    private readonly RequestDelegate _next;

    // Maps URL path prefixes to required feature flags in ConfigJson
    private static readonly Dictionary<string, string> FeatureRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/api/wms", "wms" },
        { "/api/crm", "crm" },
        { "/api/accounting", "erp" },
        { "/api/hr", "erp" },
        { "/api/admin/mes", "mes" },
        { "/api/admin/forecast", "mes" },
        { "/api/scm", "scm" },
        { "/api/pms", "pms" },
    };

    public FeatureGatingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";

        foreach (var (prefix, feature) in FeatureRoutes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var tenant = tenantContext.Tenant;
                if (tenant == null)
                {
                    await _next(context);
                    return;
                }

                if (!IsFeatureEnabled(tenant.ConfigJson, feature))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = $"Feature '{feature.ToUpper()}' is not enabled for this tenant.",
                        feature,
                        upgrade = "Please upgrade your plan or contact support."
                    });
                    return;
                }
                break;
            }
        }

        await _next(context);
    }

    private static bool IsFeatureEnabled(string? configJson, string feature)
    {
        if (string.IsNullOrEmpty(configJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("features", out var features))
            {
                if (features.TryGetProperty(feature, out var value))
                {
                    return value.ValueKind == JsonValueKind.True;
                }
            }
        }
        catch
        {
            // Invalid JSON — deny access
        }

        return false;
    }
}
