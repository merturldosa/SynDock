using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.API.Middleware;

public class ActivityTrackingMiddleware
{
    private readonly RequestDelegate _next;

    // Only track meaningful user actions
    private static readonly HashSet<string> TrackedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/products", "/api/order", "/api/cart", "/api/review",
        "/api/qna", "/api/wishlist", "/api/mall/products",
    };

    public ActivityTrackingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ShopDbContext db)
    {
        await _next(context);

        // Only track authenticated GET/POST requests that succeeded
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300) return;

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return;

        var path = context.Request.Path.Value?.ToLower() ?? "";
        var isTracked = TrackedPaths.Any(tp => path.StartsWith(tp));
        if (!isTracked) return;

        var tenantSlug = context.User?.FindFirst("tenant_slug")?.Value ?? "platform";
        var method = context.Request.Method;

        var eventType = (method, path) switch
        {
            ("GET", var p) when p.Contains("/products") => "ProductView",
            ("POST", var p) when p.Contains("/cart") => "AddToCart",
            ("POST", var p) when p.Contains("/order") => "Purchase",
            ("POST", var p) when p.Contains("/review") => "Review",
            ("POST", var p) when p.Contains("/wishlist") => "Wishlist",
            _ => null
        };

        if (eventType == null) return;

        try
        {
            db.CustomerJourneyEvents.Add(new CustomerJourneyEvent
            {
                TenantId = 0, // Platform-level tracking (cross-tenant)
                UserId = int.Parse(userId),
                EventType = eventType,
                EventDetail = path,
                Channel = context.Request.Headers["User-Agent"].ToString().Contains("Mobile") ? "Mobile" : "Web",
                SessionId = context.Connection.Id,
                CreatedBy = "ActivityTracker"
            });
            await db.SaveChangesAsync();
        }
        catch { /* Tracking failure should never break the request */ }
    }
}
