namespace Shop.API.Middleware;

public class DepartmentAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, string[]> DepartmentRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Warehouse", new[] { "/api/wms" } },
        { "Accounting", new[] { "/api/accounting", "/api/hr" } },
        { "CS", new[] { "/api/crm" } },
        { "Production", new[] { "/api/admin/mes", "/api/admin/forecast" } },
        { "Sales", new[] { "/api/scm", "/api/order" } },
        { "Marketing", new[] { "/api/crm", "/api/post", "/api/coupon" } },
    };

    public DepartmentAuthorizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var departmentClaim = context.User?.FindFirst("department")?.Value;

        // No department restriction = full access
        if (string.IsNullOrEmpty(departmentClaim) || !DepartmentRoutes.ContainsKey(departmentClaim))
        {
            await _next(context);
            return;
        }

        // Check if the path is a module-specific route
        var isModuleRoute = DepartmentRoutes.Values.SelectMany(v => v).Any(r => path.StartsWith(r));
        if (!isModuleRoute)
        {
            await _next(context);
            return;
        }

        // Check if user's department allows this route
        var allowedRoutes = DepartmentRoutes[departmentClaim];
        var hasAccess = allowedRoutes.Any(r => path.StartsWith(r));

        // Also allow common routes (products, categories, dashboard)
        var commonRoutes = new[] { "/api/products", "/api/categories", "/api/tenant-settings", "/api/admin" };
        if (!hasAccess && !commonRoutes.Any(r => path.StartsWith(r)))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"Access denied. Your department ({departmentClaim}) does not have access to this module.",
                department = departmentClaim,
                allowedModules = allowedRoutes
            });
            return;
        }

        await _next(context);
    }
}
