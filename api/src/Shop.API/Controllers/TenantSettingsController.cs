using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/tenant-settings")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class TenantSettingsController : ControllerBase
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubscriptionService _subscription;

    public TenantSettingsController(IShopDbContext db, ICurrentUserService currentUser, ISubscriptionService subscription)
    {
        _db = db;
        _currentUser = currentUser;
        _subscription = subscription;
    }

    // R-2: Usage Dashboard
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        if (tenantId == 0) return NotFound();
        return Ok(await _subscription.GetUsageSummaryAsync(tenantId, ct));
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        if (tenantId == 0) return NotFound();
        var plan = await _subscription.GetCurrentPlanAsync(tenantId, ct);
        return plan == null ? NotFound() : Ok(plan);
    }

    [HttpPut("plan")]
    public async Task<IActionResult> ChangePlan([FromBody] ChangePlanRequest req, CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        await _subscription.ChangePlanAsync(tenantId, req.PlanType, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Plan changed" });
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        return Ok(await _subscription.GetInvoicesAsync(tenantId, ct));
    }

    // R-3: Theme Customizer
    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme(CancellationToken ct)
    {
        var tenant = await GetTenantAsync(ct);
        if (tenant == null) return NotFound();

        var config = ParseConfig(tenant.ConfigJson);
        return Ok(config?.GetProperty("theme"));
    }

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest req, CancellationToken ct)
    {
        var tenant = await GetTenantAsync(ct);
        if (tenant == null) return NotFound();

        var config = string.IsNullOrEmpty(tenant.ConfigJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.ConfigJson) ?? new();

        config["theme"] = new
        {
            primaryColor = req.PrimaryColor ?? "#3B82F6",
            secondaryColor = req.SecondaryColor ?? "#1E40AF",
            fontFamily = req.FontFamily ?? "Inter, sans-serif",
            logoUrl = req.LogoUrl ?? "",
            faviconUrl = req.FaviconUrl ?? ""
        };

        tenant.ConfigJson = JsonSerializer.Serialize(config);
        tenant.UpdatedBy = _currentUser.Username;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Theme updated" });
    }

    // R-4: Onboarding Status
    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboardingStatus(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        if (tenantId == 0) return NotFound();

        var hasProducts = await _db.Products.AnyAsync(p => p.TenantId == tenantId, ct);
        var hasCategories = await _db.Categories.CountAsync(c => c.TenantId == tenantId, ct) > 1;
        var hasPayment = true; // TossPayments configured by default
        var hasLogo = false;
        var tenant = await GetTenantAsync(ct);
        if (tenant?.ConfigJson != null)
        {
            try
            {
                var doc = JsonDocument.Parse(tenant.ConfigJson);
                if (doc.RootElement.TryGetProperty("theme", out var theme) && theme.TryGetProperty("logoUrl", out var logo))
                    hasLogo = !string.IsNullOrEmpty(logo.GetString());
            }
            catch { }
        }

        var hasOrders = await _db.Orders.AnyAsync(o => o.TenantId == tenantId, ct);

        return Ok(new
        {
            steps = new[]
            {
                new { step = "profile", label = "기본 정보 설정", completed = true },
                new { step = "logo", label = "로고 업로드", completed = hasLogo },
                new { step = "categories", label = "카테고리 설정", completed = hasCategories },
                new { step = "products", label = "상품 등록", completed = hasProducts },
                new { step = "payment", label = "결제 설정", completed = hasPayment },
                new { step = "firstOrder", label = "첫 주문 받기", completed = hasOrders },
            },
            completionRate = (new[] { true, hasLogo, hasCategories, hasProducts, hasPayment, hasOrders }).Count(b => b) * 100 / 6
        });
    }

    // GAP-3: Custom Domain Management
    [HttpGet("domain")]
    public async Task<IActionResult> GetDomain(CancellationToken ct)
    {
        var tenant = await GetTenantAsync(ct);
        if (tenant == null) return NotFound();
        return Ok(new
        {
            customDomain = tenant.CustomDomain,
            subdomain = tenant.Subdomain,
            shopUrl = $"https://{tenant.Subdomain}.syndock.com",
            customDomainUrl = tenant.CustomDomain != null ? $"https://{tenant.CustomDomain}" : null,
            dnsInstructions = tenant.CustomDomain != null ? new[]
            {
                new { type = "CNAME", host = tenant.CustomDomain, target = "syndock.com" },
                new { type = "TXT", host = $"_verify.{tenant.CustomDomain}", target = $"syndock-verify={tenant.Slug}" }
            } : Array.Empty<object>()
        });
    }

    [HttpPut("domain")]
    public async Task<IActionResult> SetCustomDomain([FromBody] SetDomainRequest req, CancellationToken ct)
    {
        var tenant = await GetTenantAsync(ct);
        if (tenant == null) return NotFound();

        // Check domain uniqueness
        if (!string.IsNullOrEmpty(req.CustomDomain))
        {
            var domainTaken = await _db.Tenants.AnyAsync(t => t.CustomDomain == req.CustomDomain && t.Id != tenant.Id, ct);
            if (domainTaken) return BadRequest(new { error = "Domain is already in use by another tenant" });
        }

        tenant.CustomDomain = string.IsNullOrWhiteSpace(req.CustomDomain) ? null : req.CustomDomain.Trim().ToLower();
        tenant.UpdatedBy = _currentUser.Username;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "Custom domain updated",
            customDomain = tenant.CustomDomain,
            nextSteps = tenant.CustomDomain != null
                ? new[] {
                    $"1. DNS에 CNAME 레코드 추가: {tenant.CustomDomain} → syndock.com",
                    $"2. SSL 인증서 자동 발급을 위해 최대 10분 소요",
                    $"3. https://{tenant.CustomDomain} 접속 확인"
                }
                : Array.Empty<string>()
        });
    }

    // Features toggle
    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures(CancellationToken ct)
    {
        var tenant = await GetTenantAsync(ct);
        if (tenant == null) return NotFound();
        var config = ParseConfig(tenant.ConfigJson);
        if (config == null) return Ok(new { });
        return config.Value.TryGetProperty("features", out var features) ? Ok(features) : Ok(new { });
    }

    private async Task<int> GetTenantIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? 0;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.TenantId ?? 0;
    }

    private async Task<Shop.Domain.Entities.Tenant?> GetTenantAsync(CancellationToken ct)
    {
        var tenantId = await GetTenantIdAsync(ct);
        return await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
    }

    private static JsonElement? ParseConfig(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonDocument.Parse(json).RootElement; } catch { return null; }
    }
}

public record ChangePlanRequest(string PlanType);
public record UpdateThemeRequest(string? PrimaryColor, string? SecondaryColor, string? FontFamily, string? LogoUrl, string? FaviconUrl);
public record SetDomainRequest(string? CustomDomain);
