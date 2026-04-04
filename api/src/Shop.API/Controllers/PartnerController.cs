using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;
using System.Security.Cryptography;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/partner")]
public class PartnerController : ControllerBase
{
    private readonly ShopDbContext _db;

    public PartnerController(ShopDbContext db) => _db = db;

    // === Public Endpoints ===

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "SynDock Partner API", version = "1.0" });

    [HttpGet("docs")]
    public IActionResult Docs() => Ok(new
    {
        name = "SynDock Partner API",
        version = "1.0",
        authentication = new
        {
            method = "HMAC-SHA256",
            headers = new
            {
                required = new[] { "X-Partner-Key: your_api_key" },
                recommended = new[] { "X-Partner-Signature: hmac_sha256(method+path+timestamp+body, secret)", "X-Partner-Timestamp: unix_epoch_seconds" }
            }
        },
        endpoints = new[]
        {
            new { method = "POST", path = "/api/partner/products", desc = "Register product on SynDock Mall" },
            new { method = "PUT", path = "/api/partner/products/{externalId}", desc = "Update product" },
            new { method = "DELETE", path = "/api/partner/products/{externalId}", desc = "Remove product" },
            new { method = "GET", path = "/api/partner/products", desc = "List your products" },
            new { method = "PUT", path = "/api/partner/products/{externalId}/stock", desc = "Update stock" },
            new { method = "GET", path = "/api/partner/orders", desc = "Get orders for your products" },
            new { method = "PUT", path = "/api/partner/orders/{orderId}/ship", desc = "Mark order as shipped" },
            new { method = "GET", path = "/api/partner/stats", desc = "Your dashboard stats" },
        },
        rateLimit = new { perMinute = 60, perDay = 10000 },
        security = new[] { "HMAC-SHA256 signature", "IP whitelist", "Timestamp validation (±5 min)", "Audit logging" }
    });

    // === Partner Product Endpoints (secured by PartnerApiAuthMiddleware) ===

    [HttpPost("products")]
    public async Task<IActionResult> RegisterProduct([FromBody] PartnerProductRequest req, CancellationToken ct)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var autoApprove = (bool)HttpContext.Items["AutoApprove"]!;

        // Duplicate check
        var exists = await _db.PartnerProducts
            .AnyAsync(p => p.ApiPartnerId == partnerId && p.ExternalProductId == req.ExternalProductId, ct);
        if (exists)
            return Conflict(new { error = "Product already registered", externalId = req.ExternalProductId });

        // Content validation
        if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 2)
            return BadRequest(new { error = "Product name must be at least 2 characters" });
        if (req.Price <= 0)
            return BadRequest(new { error = "Price must be positive" });

        var product = new PartnerProduct
        {
            ApiPartnerId = partnerId,
            ExternalProductId = req.ExternalProductId,
            Name = SanitizeInput(req.Name),
            Description = req.Description != null ? SanitizeInput(req.Description) : null,
            Price = req.Price,
            SalePrice = req.SalePrice,
            Category = req.Category,
            ImageUrl = ValidateUrl(req.ImageUrl),
            ProductUrl = ValidateUrl(req.ProductUrl),
            Stock = Math.Max(0, req.Stock),
            Sku = req.Sku,
            Brand = req.Brand,
            ApprovalStatus = autoApprove ? "Approved" : "Pending",
            ApprovedAt = autoApprove ? DateTime.UtcNow : null,
            ApprovedBy = autoApprove ? "auto" : null,
            AttributesJson = req.AttributesJson,
            CreatedBy = HttpContext.Items["PartnerCode"]?.ToString() ?? "partner"
        };

        _db.PartnerProducts.Add(product);

        // If auto-approved, create SynDock product on Mall
        if (autoApprove)
        {
            var synDockProduct = new Product
            {
                TenantId = 0, // Platform-level (Mall)
                Name = product.Name,
                Slug = $"partner-{partnerId}-{product.ExternalProductId}",
                Description = product.Description,
                Price = product.Price,
                SalePrice = product.SalePrice,
                PriceType = "Fixed",
                IsActive = true,
                IsNew = true,
                SourceId = $"partner-{partnerId}",
                CreatedBy = "PartnerAPI"
            };
            _db.Products.Add(synDockProduct);
            await _db.SaveChangesAsync(ct);
            product.SynDockProductId = synDockProduct.Id;
        }

        var partnerEntity = await _db.ApiPartners.FindAsync(partnerId);
        if (partnerEntity != null) partnerEntity.TotalProducts++;

        await _db.SaveChangesAsync(ct);

        return Created($"/api/partner/products/{product.ExternalProductId}", new
        {
            externalId = product.ExternalProductId,
            synDockProductId = product.SynDockProductId,
            approvalStatus = product.ApprovalStatus,
            message = autoApprove ? "Product listed on SynDock Mall" : "Product submitted for review"
        });
    }

    [HttpPut("products/{externalId}")]
    public async Task<IActionResult> UpdateProduct(string externalId, [FromBody] PartnerProductUpdateRequest req, CancellationToken ct)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var product = await _db.PartnerProducts.FirstOrDefaultAsync(p => p.ApiPartnerId == partnerId && p.ExternalProductId == externalId, ct);
        if (product == null) return NotFound(new { error = "Product not found" });

        if (req.Name != null) product.Name = SanitizeInput(req.Name);
        if (req.Description != null) product.Description = SanitizeInput(req.Description);
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.SalePrice.HasValue) product.SalePrice = req.SalePrice;
        if (req.Stock.HasValue) product.Stock = Math.Max(0, req.Stock.Value);
        if (req.ImageUrl != null) product.ImageUrl = ValidateUrl(req.ImageUrl);
        product.UpdatedBy = HttpContext.Items["PartnerCode"]?.ToString();
        product.UpdatedAt = DateTime.UtcNow;

        // Sync to SynDock product if approved
        if (product.SynDockProductId.HasValue)
        {
            var sp = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.SynDockProductId.Value, ct);
            if (sp != null)
            {
                if (req.Name != null) sp.Name = product.Name;
                if (req.Description != null) sp.Description = product.Description;
                if (req.Price.HasValue) sp.Price = product.Price;
                if (req.SalePrice.HasValue) sp.SalePrice = product.SalePrice;
                sp.UpdatedBy = "PartnerAPI";
                sp.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { externalId, message = "Product updated" });
    }

    [HttpDelete("products/{externalId}")]
    public async Task<IActionResult> RemoveProduct(string externalId, CancellationToken ct)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var product = await _db.PartnerProducts.FirstOrDefaultAsync(p => p.ApiPartnerId == partnerId && p.ExternalProductId == externalId, ct);
        if (product == null) return NotFound();

        product.ApprovalStatus = "Suspended";
        if (product.SynDockProductId.HasValue)
        {
            var sp = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.SynDockProductId.Value, ct);
            if (sp != null) sp.IsActive = false;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { externalId, message = "Product removed" });
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var query = _db.PartnerProducts.AsNoTracking().Where(p => p.ApiPartnerId == partnerId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.ApprovalStatus == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpPut("products/{externalId}/stock")]
    public async Task<IActionResult> UpdateStock(string externalId, [FromBody] StockUpdateRequest req, CancellationToken ct)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var product = await _db.PartnerProducts.FirstOrDefaultAsync(p => p.ApiPartnerId == partnerId && p.ExternalProductId == externalId, ct);
        if (product == null) return NotFound();

        product.Stock = Math.Max(0, req.Stock);
        product.UpdatedAt = DateTime.UtcNow;

        // Sync stock to SynDock product variant
        if (product.SynDockProductId.HasValue)
        {
            var variant = await _db.ProductVariants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.ProductId == product.SynDockProductId.Value, ct);
            if (variant != null) variant.Stock = product.Stock;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { externalId, stock = product.Stock });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        // Return orders that contain partner products (via MarketplaceOrders or direct Mall orders)
        var partnerProductIds = await _db.PartnerProducts.Where(p => p.ApiPartnerId == partnerId && p.SynDockProductId != null)
            .Select(p => p.SynDockProductId!.Value).ToListAsync(ct);

        var orders = await _db.OrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(oi => partnerProductIds.Contains(oi.ProductId))
            .Include(oi => oi.Order)
            .OrderByDescending(oi => oi.Order.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(oi => new
            {
                orderId = oi.Order.Id,
                orderNumber = oi.Order.OrderNumber,
                productId = oi.ProductId,
                productName = oi.ProductName,
                quantity = oi.Quantity,
                unitPrice = oi.UnitPrice,
                totalPrice = oi.TotalPrice,
                orderStatus = oi.Order.Status,
                orderedAt = oi.Order.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items = orders, totalCount = orders.Count, page, pageSize });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var partnerId = (int)HttpContext.Items["PartnerId"]!;
        var partner = await _db.ApiPartners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partnerId, ct);
        var products = await _db.PartnerProducts.Where(p => p.ApiPartnerId == partnerId).GroupBy(p => p.ApprovalStatus)
            .Select(g => new { status = g.Key, count = g.Count() }).ToListAsync(ct);
        var todayCalls = await _db.PartnerApiLogs.CountAsync(l => l.ApiPartnerId == partnerId && l.CreatedAt > DateTime.UtcNow.Date, ct);

        return Ok(new
        {
            partner = new { partner?.CompanyName, partner?.Tier, partner?.Status, partner?.CommissionRate },
            products,
            totalProducts = partner?.TotalProducts ?? 0,
            totalOrders = partner?.TotalOrders ?? 0,
            todayApiCalls = todayCalls,
            rateLimits = new { perMinute = partner?.RateLimitPerMinute, perDay = partner?.RateLimitPerDay, used = partner?.DailyCallCount }
        });
    }

    // === PlatformAdmin Endpoints (JWT auth) ===

    [HttpPost("admin/register")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> RegisterPartner([FromBody] RegisterPartnerRequest req, CancellationToken ct)
    {
        var apiKey = $"sdk_{Guid.NewGuid():N}";
        var apiSecret = $"sds_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";

        var partner = new ApiPartner
        {
            CompanyName = req.CompanyName,
            PartnerCode = req.PartnerCode.ToLower(),
            ApiKey = apiKey,
            ApiSecretHash = apiSecret, // In production: store hash only
            Status = "Active",
            Tier = req.Tier ?? "Standard",
            ContactEmail = req.ContactEmail,
            AllowedIps = req.AllowedIps,
            RateLimitPerMinute = req.Tier == "Enterprise" ? 300 : req.Tier == "Premium" ? 120 : 60,
            RateLimitPerDay = req.Tier == "Enterprise" ? 100000 : req.Tier == "Premium" ? 50000 : 10000,
            CommissionRate = req.CommissionRate ?? 3.0m,
            AutoApproveProducts = req.Tier == "Enterprise",
            CreatedBy = "PlatformAdmin"
        };

        _db.ApiPartners.Add(partner);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            partnerId = partner.Id,
            partnerCode = partner.PartnerCode,
            apiKey,
            apiSecret,
            tier = partner.Tier,
            rateLimit = new { perMinute = partner.RateLimitPerMinute, perDay = partner.RateLimitPerDay },
            warning = "Save the API Secret now. It cannot be retrieved later."
        });
    }

    [HttpGet("admin/partners")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> GetPartners(CancellationToken ct)
        => Ok(await _db.ApiPartners.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(ct));

    [HttpGet("admin/logs")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> GetLogs([FromQuery] int? partnerId, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var query = _db.PartnerApiLogs.AsNoTracking().AsQueryable();
        if (partnerId.HasValue) query = query.Where(l => l.ApiPartnerId == partnerId.Value);
        return Ok(await query.OrderByDescending(l => l.CreatedAt).Take(limit).ToListAsync(ct));
    }

    [HttpPost("admin/products/{id}/approve")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> ApproveProduct(int id, CancellationToken ct)
    {
        var product = await _db.PartnerProducts.Include(p => p.Partner).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product == null) return NotFound();

        product.ApprovalStatus = "Approved";
        product.ApprovedAt = DateTime.UtcNow;
        product.ApprovedBy = "PlatformAdmin";

        // Create SynDock Mall product
        var synDockProduct = new Product
        {
            TenantId = 0,
            Name = product.Name,
            Slug = $"partner-{product.ApiPartnerId}-{product.ExternalProductId}",
            Description = product.Description,
            Price = product.Price,
            SalePrice = product.SalePrice,
            PriceType = "Fixed",
            IsActive = true,
            IsNew = true,
            SourceId = $"partner-{product.ApiPartnerId}",
            CreatedBy = "PartnerAPI"
        };
        _db.Products.Add(synDockProduct);
        await _db.SaveChangesAsync(ct);
        product.SynDockProductId = synDockProduct.Id;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Product approved and listed on Mall", synDockProductId = synDockProduct.Id });
    }

    [HttpPost("admin/products/{id}/reject")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> RejectProduct(int id, [FromBody] RejectProductRequest req, CancellationToken ct)
    {
        var product = await _db.PartnerProducts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product == null) return NotFound();
        product.ApprovalStatus = "Rejected";
        product.RejectionReason = req.Reason;
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Product rejected" });
    }

    // === Helpers ===

    private static string SanitizeInput(string input)
    {
        // Remove HTML tags, scripts, SQL injection patterns
        var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"<[^>]*>", "");
        sanitized = sanitized.Replace("'", "").Replace("\"", "").Replace(";", "").Replace("--", "");
        return sanitized.Trim();
    }

    private static string? ValidateUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            return uri.ToString();
        return null;
    }
}

// Request DTOs
public record PartnerProductRequest(string ExternalProductId, string Name, string? Description, decimal Price, decimal? SalePrice, string? Category, string? ImageUrl, string? ProductUrl, int Stock = 0, string? Sku = null, string? Brand = null, string? AttributesJson = null);
public record PartnerProductUpdateRequest(string? Name = null, string? Description = null, decimal? Price = null, decimal? SalePrice = null, int? Stock = null, string? ImageUrl = null);
public record StockUpdateRequest(int Stock);
public record RegisterPartnerRequest(string CompanyName, string PartnerCode, string? Tier = null, string? ContactEmail = null, string? AllowedIps = null, decimal? CommissionRate = null);
public record RejectProductRequest(string Reason);
