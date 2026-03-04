using System.Security.Cryptography;
using System.Text;

namespace Shop.API.Middleware;

public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, (int MaxAge, bool IsPublic)> CacheRules = new()
    {
        // Public GET endpoints with cache durations in seconds
        ["/api/products"] = (300, true),           // 5 min
        ["/api/products/slugs"] = (3600, true),    // 1 hour
        ["/api/categories"] = (600, true),          // 10 min
        ["/api/categories/slugs"] = (3600, true),  // 1 hour
        ["/api/currency/rates"] = (21600, true),   // 6 hours
    };

    public ResponseCachingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method;

        // Only apply caching to GET requests
        if (method != "GET")
        {
            await _next(context);
            return;
        }

        // Static files
        if (path.StartsWith("/uploads/") || path.StartsWith("/icons/"))
        {
            context.Response.Headers["Cache-Control"] = "public, max-age=2592000, immutable"; // 30 days
            await _next(context);
            return;
        }

        // Check cache rules for API paths
        var matchedRule = CacheRules.FirstOrDefault(r => path.StartsWith(r.Key));
        if (matchedRule.Key != null)
        {
            var (maxAge, isPublic) = matchedRule.Value;
            var visibility = isPublic ? "public" : "private";
            context.Response.Headers["Cache-Control"] = $"{visibility}, max-age={maxAge}";
            context.Response.Headers["Vary"] = "Accept-Language, X-Tenant-Id";

            // Capture response for ETag
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context);

            memStream.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(memStream).ReadToEndAsync();

            // Generate ETag
            var etag = GenerateETag(body);
            context.Response.Headers["ETag"] = etag;

            // Check If-None-Match
            var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.Body = originalBody;
                return;
            }

            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
            return;
        }

        // Default: no-cache for other API endpoints
        if (path.StartsWith("/api/"))
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        }

        await _next(context);
    }

    private static string GenerateETag(string content)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(content));
        return $"\"{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}\"";
    }
}
