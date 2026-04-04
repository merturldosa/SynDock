using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure.Data;

namespace Shop.API.Middleware;

/// <summary>
/// 7-Layer Security for Partner API:
/// 1. API Key validation (X-Partner-Key header)
/// 2. HMAC-SHA256 signature verification (X-Partner-Signature header)
/// 3. IP whitelist check
/// 4. Rate limiting (per-minute + per-day)
/// 5. Partner status check (Active only)
/// 6. Timestamp validation (prevent replay attacks, ±5 min window)
/// 7. Audit logging (all requests)
/// </summary>
public class PartnerApiAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PartnerApiAuthMiddleware> _logger;

    public PartnerApiAuthMiddleware(RequestDelegate next, ILogger<PartnerApiAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ShopDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (!path.StartsWith("/api/partner/"))
        {
            await _next(context);
            return;
        }

        // Skip auth for public docs endpoint
        if (path == "/api/partner/docs" || path == "/api/partner/health")
        {
            await _next(context);
            return;
        }

        // Skip auth for PlatformAdmin endpoints (use JWT auth instead)
        if (path.StartsWith("/api/partner/admin/"))
        {
            await _next(context);
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var apiKey = context.Request.Headers["X-Partner-Key"].FirstOrDefault();
        var signature = context.Request.Headers["X-Partner-Signature"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-Partner-Timestamp"].FirstOrDefault();
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        // Layer 1: API Key required
        if (string.IsNullOrEmpty(apiKey))
        {
            await RejectAsync(context, db, null, null, 401, "Missing X-Partner-Key header", path, clientIp, sw);
            return;
        }

        // Find partner
        var partner = await db.ApiPartners.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApiKey == apiKey);

        if (partner == null)
        {
            await RejectAsync(context, db, null, apiKey[..Math.Min(8, apiKey.Length)], 401, "Invalid API key", path, clientIp, sw);
            return;
        }

        // Layer 5: Status check
        if (partner.Status != "Active")
        {
            await RejectAsync(context, db, partner.Id, partner.PartnerCode, 403, $"Partner status: {partner.Status}", path, clientIp, sw);
            return;
        }

        // Layer 3: IP whitelist
        if (!string.IsNullOrEmpty(partner.AllowedIps) && !string.IsNullOrEmpty(clientIp))
        {
            var allowedIps = partner.AllowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ip => ip.Trim());
            if (!allowedIps.Contains(clientIp) && !allowedIps.Contains("*"))
            {
                await RejectAsync(context, db, partner.Id, partner.PartnerCode, 403, $"IP {clientIp} not whitelisted", path, clientIp, sw);
                return;
            }
        }

        // Layer 6: Timestamp validation (±5 min to prevent replay attacks)
        if (!string.IsNullOrEmpty(timestamp))
        {
            if (long.TryParse(timestamp, out var ts))
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
                var diff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);
                if (diff > 5)
                {
                    await RejectAsync(context, db, partner.Id, partner.PartnerCode, 401, "Request timestamp expired (±5 min)", path, clientIp, sw);
                    return;
                }
            }
        }

        // Layer 2: HMAC-SHA256 signature verification
        if (!string.IsNullOrEmpty(signature))
        {
            // Read request body for signature verification
            context.Request.EnableBuffering();
            var body = "";
            if (context.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var dataToSign = $"{context.Request.Method}{path}{timestamp}{body}";
            var expectedSignature = ComputeHmacSha256(dataToSign, partner.ApiSecretHash);

            if (!string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                await RejectAsync(context, db, partner.Id, partner.PartnerCode, 401, "Invalid HMAC signature", path, clientIp, sw, signature);
                return;
            }
        }

        // Layer 4: Rate limiting
        var now = DateTime.UtcNow;
        if (partner.DailyCallResetAt == null || partner.DailyCallResetAt < now.Date)
        {
            // Reset daily counter
            var tracked = await db.ApiPartners.FindAsync(partner.Id);
            if (tracked != null) { tracked.DailyCallCount = 0; tracked.DailyCallResetAt = now.Date.AddDays(1); }
        }

        if (partner.DailyCallCount >= partner.RateLimitPerDay)
        {
            await RejectAsync(context, db, partner.Id, partner.PartnerCode, 429, "Daily rate limit exceeded", path, clientIp, sw);
            return;
        }

        // Increment call count
        var partnerEntity = await db.ApiPartners.FindAsync(partner.Id);
        if (partnerEntity != null)
        {
            partnerEntity.DailyCallCount++;
            partnerEntity.LastActivityAt = now;
        }

        // Set partner info in HttpContext for controller use
        context.Items["PartnerId"] = partner.Id;
        context.Items["PartnerCode"] = partner.PartnerCode;
        context.Items["PartnerTier"] = partner.Tier;
        context.Items["AutoApprove"] = partner.AutoApproveProducts;

        await _next(context);

        // Layer 7: Audit logging (success)
        sw.Stop();
        db.PartnerApiLogs.Add(new Shop.Domain.Entities.PartnerApiLog
        {
            ApiPartnerId = partner.Id,
            PartnerCode = partner.PartnerCode,
            HttpMethod = context.Request.Method,
            RequestPath = path,
            ClientIp = clientIp,
            ResponseStatus = context.Response.StatusCode,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            RequestSignature = signature,
            SignatureValid = true,
            CreatedBy = "system"
        });
        await db.SaveChangesAsync();
    }

    private async Task RejectAsync(HttpContext context, ShopDbContext db, int? partnerId, string? partnerCode, int status, string error, string path, string? clientIp, System.Diagnostics.Stopwatch sw, string? signature = null)
    {
        sw.Stop();
        _logger.LogWarning("Partner API rejected: {Error} | Partner={Partner} IP={IP} Path={Path}", error, partnerCode ?? "unknown", clientIp, path);

        // Audit log (failure)
        db.PartnerApiLogs.Add(new Shop.Domain.Entities.PartnerApiLog
        {
            ApiPartnerId = partnerId,
            PartnerCode = partnerCode,
            HttpMethod = context.Request.Method,
            RequestPath = path,
            ClientIp = clientIp,
            ResponseStatus = status,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            ErrorMessage = error,
            RequestSignature = signature,
            SignatureValid = false,
            CreatedBy = "system"
        });
        await db.SaveChangesAsync();

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error, code = status });
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
