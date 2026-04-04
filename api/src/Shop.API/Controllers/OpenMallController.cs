using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using Shop.Infrastructure.Data;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class OpenMallController : ControllerBase
{
    private readonly ShopDbContext _db;

    public OpenMallController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>OpenMall: Browse all products across all tenants</summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? tenantSlug,
        [FromQuery] string? sortBy, // price-asc, price-desc, newest, popular
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        var query = _db.Products.IgnoreQueryFilters()
            .Where(p => p.IsActive && p.Tenant.IsActive)
            .Include(p => p.Tenant)
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder).Take(1))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.Slug == category || p.Category.Name.Contains(category));
        if (!string.IsNullOrWhiteSpace(tenantSlug))
            query = query.Where(p => p.Tenant.Slug == tenantSlug);
        if (minPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.Price) >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.Price) <= maxPrice.Value);

        query = sortBy switch
        {
            "price-asc" => query.OrderBy(p => p.SalePrice ?? p.Price),
            "price-desc" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
            "popular" => query.OrderByDescending(p => p.ViewCount),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.SalePrice,
                p.IsFeatured,
                p.IsNew,
                category = p.Category != null ? p.Category.Name : null,
                imageUrl = p.Images.Any() ? p.Images.First().Url : null,
                tenant = new { p.Tenant.Slug, p.Tenant.Name, p.Tenant.ConfigJson },
                shopUrl = $"https://{p.Tenant.Slug}.syndock.com"
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    /// <summary>OpenMall: Product detail</summary>
    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        var product = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.Id == id && p.IsActive)
            .Include(p => p.Tenant)
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.DetailSections.OrderBy(s => s.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (product == null) return NotFound();

        // Get reviews separately
        var reviews = await _db.Reviews.IgnoreQueryFilters()
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt).Take(5)
            .Select(r => new { r.Rating, r.Content, r.CreatedAt })
            .ToListAsync(ct);
        var avgRating = await _db.Reviews.IgnoreQueryFilters()
            .Where(r => r.ProductId == id)
            .AverageAsync(r => (double?)r.Rating, ct) ?? 0;
        var reviewCount = await _db.Reviews.IgnoreQueryFilters()
            .CountAsync(r => r.ProductId == id, ct);

        // Increment view count
        var tracked = await _db.Products.IgnoreQueryFilters().FirstAsync(p => p.Id == id, ct);
        tracked.ViewCount++;
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            product.Id, product.Name, product.Slug, product.Description,
            product.Price, product.SalePrice, product.Specification,
            product.IsFeatured, product.IsNew, product.ViewCount,
            category = product.Category?.Name,
            images = product.Images.Select(i => new { i.Url, i.SortOrder }),
            variants = product.Variants.Select(v => new { v.Id, v.Name, v.Price, v.Stock, v.Sku }),
            sections = product.DetailSections.Select(s => new { s.Title, s.Content }),
            reviews,
            avgRating = Math.Round(avgRating, 1),
            reviewCount,
            tenant = new
            {
                product.Tenant.Slug, product.Tenant.Name,
                shopUrl = $"https://{product.Tenant.Slug}.syndock.com"
            }
        });
    }

    /// <summary>OpenMall: Browse all tenant shops</summary>
    [HttpGet("shops")]
    public async Task<IActionResult> GetShops(CancellationToken ct)
    {
        var shops = await _db.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new
            {
                t.Slug, t.Name, t.ConfigJson,
                productCount = _db.Products.IgnoreQueryFilters().Count(p => p.TenantId == t.Id && p.IsActive),
                shopUrl = $"https://{t.Slug}.syndock.com"
            })
            .ToListAsync(ct);

        return Ok(shops);
    }

    /// <summary>OpenMall: New arrivals from all shops (last 7 days)</summary>
    [HttpGet("new-arrivals")]
    public async Task<IActionResult> GetNewArrivals([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var products = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.IsActive && p.Tenant.IsActive && p.CreatedAt > since)
            .Include(p => p.Tenant)
            .Include(p => p.Images.OrderBy(i => i.SortOrder).Take(1))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, p.Price, p.SalePrice,
                imageUrl = p.Images.Any() ? p.Images.First().Url : null,
                tenant = new { p.Tenant.Slug, p.Tenant.Name },
                registeredAt = p.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(products);
    }

    /// <summary>OpenMall: Featured products from all shops</summary>
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var featured = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.IsActive && p.IsFeatured && p.Tenant.IsActive)
            .Include(p => p.Tenant).Include(p => p.Images.OrderBy(i => i.SortOrder).Take(1))
            .OrderByDescending(p => p.ViewCount)
            .Take(20)
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, p.Price, p.SalePrice,
                imageUrl = p.Images.Any() ? p.Images.First().Url : null,
                tenant = new { p.Tenant.Slug, p.Tenant.Name }
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(featured);
    }

    /// <summary>OpenMall: Search suggestions</summary>
    [HttpGet("search/suggest")]
    public async Task<IActionResult> SearchSuggest([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Ok(new { suggestions = Array.Empty<string>() });

        var products = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.IsActive && p.Name.Contains(q))
            .Select(p => p.Name)
            .Distinct().Take(10)
            .ToListAsync(ct);

        var categories = await _db.Categories.IgnoreQueryFilters()
            .Where(c => c.IsActive && c.Name.Contains(q))
            .Select(c => c.Name)
            .Distinct().Take(5)
            .ToListAsync(ct);

        return Ok(new { products, categories });
    }

    /// <summary>OpenMall: Statistics for homepage</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        return Ok(new
        {
            totalShops = await _db.Tenants.CountAsync(t => t.IsActive, ct),
            totalProducts = await _db.Products.IgnoreQueryFilters().CountAsync(p => p.IsActive, ct),
            totalCategories = await _db.Categories.IgnoreQueryFilters().CountAsync(c => c.IsActive, ct)
        });
    }

    /// <summary>OpenMall: Get categories across all tenants</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _db.Categories.IgnoreQueryFilters()
            .Where(c => c.IsActive && c.Tenant.IsActive && c.ParentId == null)
            .Include(c => c.Tenant)
            .AsNoTracking()
            .GroupBy(c => c.Name)
            .Select(g => new { name = g.Key, slug = g.First().Slug, count = g.Sum(c => 1), tenants = g.Select(c => c.Tenant.Name).Distinct() })
            .ToListAsync(ct);
        return Ok(categories);
    }

    /// <summary>OpenMall: Get related products from same tenant</summary>
    [HttpGet("products/{id}/related")]
    public async Task<IActionResult> GetRelatedProducts(int id, CancellationToken ct)
    {
        var product = await _db.Products.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product == null) return NotFound();

        var related = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == product.TenantId && p.Id != id && p.IsActive && p.CategoryId == product.CategoryId)
            .Include(p => p.Images.OrderBy(i => i.SortOrder).Take(1))
            .Take(8)
            .Select(p => new { p.Id, p.Name, p.Slug, p.Price, p.SalePrice, imageUrl = p.Images.Any() ? p.Images.First().Url : null })
            .AsNoTracking()
            .ToListAsync(ct);
        return Ok(related);
    }

    // ========== SSO Verify/Logout Endpoints ==========

    /// <summary>SSO: Verify token and return user info (used by all frontend apps)</summary>
    [HttpGet("sso/verify")]
    [Authorize]
    public async Task<IActionResult> VerifySso(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var userId = int.Parse(userIdClaim);

        var user = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return Unauthorized();

        return Ok(new
        {
            valid = true,
            user = new { user.Id, user.Username, user.Email, user.Name, user.Role },
            isPlatformUser = user.TenantId == 0,
            tenantId = User.FindFirst("tenant_id")?.Value,
            tenantSlug = User.FindFirst("tenant_slug")?.Value
        });
    }

    /// <summary>SSO: Logout - clear session across all apps</summary>
    [HttpPost("sso/logout")]
    public IActionResult SsoLogout()
    {
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = Request.Host.Host.EndsWith("syndock.com") ? ".syndock.com" : null,
            MaxAge = TimeSpan.Zero
        };
        Response.Cookies.Delete("syndock_sso", cookieOptions);
        Response.Cookies.Delete("syndock_user", cookieOptions);
        return Ok(new { message = "Logged out" });
    }

    // ========== SSO Auth Endpoints ==========

    /// <summary>OpenMall: Register platform user (SSO - works across all shops)</summary>
    [HttpPost("auth/register")]
    public async Task<IActionResult> Register([FromBody] MallRegisterRequest req, CancellationToken ct)
    {
        // Check if email already exists as a platform user
        var existingUser = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == req.Email && u.TenantId == 0, ct);
        if (existingUser)
            return BadRequest(new { error = "Email already registered" });

        var user = new User
        {
            TenantId = 0, // Platform user - not bound to any tenant
            Username = req.Email.Split('@')[0],
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Name = req.Name,
            Phone = req.Phone,
            Role = "Member",
            IsActive = true,
            EmailVerified = false,
            CreatedBy = "OpenMall"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Registration successful", userId = user.Id });
    }

    /// <summary>OpenMall: Login platform user (SSO token works on all tenant shops)</summary>
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] MallLoginRequest req, [FromServices] ITokenService tokenService, CancellationToken ct)
    {
        // Find platform user (TenantId = 0)
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.TenantId == 0 && u.IsActive, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password" });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Create a platform tenant stub for token generation
        var platformTenant = new Tenant { Id = 0, Slug = "platform", Name = "SynDock Platform" };
        var accessToken = tokenService.GenerateAccessToken(user, platformTenant);
        var refreshToken = tokenService.GenerateRefreshToken();

        return Ok(new
        {
            auth = new
            {
                accessToken,
                refreshToken,
                user = new { user.Id, user.Username, user.Email, user.Name, user.Phone, user.Role, isPlatformUser = true }
            }
        });
    }

    /// <summary>OpenMall: Get current user profile</summary>
    [HttpGet("auth/me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var userId = int.Parse(userIdClaim);

        var user = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return NotFound();

        // Get order history across ALL tenants
        var orderCount = await _db.Orders.IgnoreQueryFilters()
            .CountAsync(o => o.UserId == userId, ct);
        var totalSpent = await _db.Orders.IgnoreQueryFilters()
            .Where(o => o.UserId == userId && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

        return Ok(new
        {
            user.Id, user.Username, user.Email, user.Name, user.Phone, user.Role,
            isPlatformUser = user.TenantId == 0,
            orderCount, totalSpent
        });
    }

    /// <summary>OpenMall: Get user's orders across all tenant shops</summary>
    [HttpGet("auth/orders")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
        var userId = int.Parse(userIdClaim);

        var query = _db.Orders.IgnoreQueryFilters()
            .Where(o => o.UserId == userId)
            .Include(o => o.Tenant)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(ct);
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.Status, o.TotalAmount, o.CreatedAt,
                tenant = new { o.Tenant.Slug, o.Tenant.Name },
                shopUrl = $"https://{o.Tenant.Slug}.syndock.com"
            })
            .ToListAsync(ct);

        return Ok(new { orders, totalCount, page, pageSize });
    }
}

public record MallRegisterRequest(string Email, string Password, string Name, string? Phone = null);
public record MallLoginRequest(string Email, string Password);
