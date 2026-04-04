using System.Text.RegularExpressions;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Middleware;

public class SecurityGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityGuardMiddleware> _logger;

    // SQL injection patterns
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|ALTER|CREATE|EXEC|EXECUTE)\b.*\b(FROM|INTO|TABLE|DATABASE|WHERE|SET)\b)|(-{2})|(/\*.*\*/)|(\bOR\b\s+\b\d+\b\s*=\s*\b\d+\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // XSS patterns
    private static readonly Regex XssPattern = new(
        @"<\s*script|javascript\s*:|on\w+\s*=|<\s*iframe|<\s*object|<\s*embed|eval\s*\(|document\.(cookie|write)|alert\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SecurityGuardMiddleware(RequestDelegate next, ILogger<SecurityGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityMonitorService security)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        // Check 1: Is IP blocked?
        if (!string.IsNullOrEmpty(clientIp) && await security.IsIpBlockedAsync(clientIp))
        {
            _logger.LogWarning("Blocked IP attempted access: {IP}", clientIp);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Access denied", code = "IP_BLOCKED" });
            return;
        }

        // Check 2: SQL Injection in query string
        var queryString = context.Request.QueryString.Value ?? "";
        if (SqlInjectionPattern.IsMatch(queryString))
        {
            await security.RecordEventAsync("SqlInjection", "Critical", clientIp,
                context.Request.Headers.UserAgent, null, null, context.Request.Path,
                $"SQL injection attempt in query: {queryString[..Math.Min(200, queryString.Length)]}");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Malicious request detected", code = "SQL_INJECTION" });
            return;
        }

        // Check 3: XSS in query string
        if (XssPattern.IsMatch(queryString))
        {
            await security.RecordEventAsync("XssAttempt", "High", clientIp,
                context.Request.Headers.UserAgent, null, null, context.Request.Path,
                $"XSS attempt in query: {queryString[..Math.Min(200, queryString.Length)]}");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Malicious request detected", code = "XSS_ATTEMPT" });
            return;
        }

        // Check 4: XSS/SQLi in request body (POST/PUT only)
        // Note: Body scanning disabled to prevent stream conflicts with MVC model binding.
        // Query string scanning (above) catches most injection attempts.
        // For body scanning, use input validation at the controller/model level instead.
        if (false && context.Request.ContentLength > 0 && (context.Request.Method == "POST" || context.Request.Method == "PUT"))
        {
            context.Request.EnableBuffering();
            var body = "";
            using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            context.Request.Body.Position = 0;

            if (body.Length > 0 && body.Length < 10000)
            {
                if (SqlInjectionPattern.IsMatch(body))
                {
                    await security.RecordEventAsync("SqlInjection", "Critical", clientIp,
                        context.Request.Headers.UserAgent, null, null, context.Request.Path,
                        "SQL injection attempt in request body");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Malicious request detected" });
                    return;
                }
                if (XssPattern.IsMatch(body))
                {
                    await security.RecordEventAsync("XssAttempt", "High", clientIp,
                        context.Request.Headers.UserAgent, null, null, context.Request.Path,
                        "XSS attempt in request body");
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Malicious request detected" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
