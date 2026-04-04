using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/sns-marketing")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class SnsMarketingController : ControllerBase
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SnsMarketingController(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all SNS posts (pending, posted, failed)</summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? platform,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _db.SocialPosts.AsNoTracking()
            .Include(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(p => p.Platform == platform);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Id, p.Platform, p.Caption, p.ImageUrl, p.Status,
                p.PostUrl, p.PostedAt, p.CreatedAt, p.CreatedBy,
                productName = p.Product != null ? p.Product.Name : null,
                productId = p.ProductId
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    /// <summary>Approve and schedule a pending SNS post</summary>
    [HttpPost("posts/{id}/approve")]
    public async Task<IActionResult> ApprovePost(int id, CancellationToken ct)
    {
        var post = await _db.SocialPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post == null) return NotFound();

        post.Status = "Approved";
        post.UpdatedBy = _currentUser.Username;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Post approved for publishing", platform = post.Platform });
    }

    /// <summary>Edit post caption before publishing</summary>
    [HttpPut("posts/{id}")]
    public async Task<IActionResult> EditPost(int id, [FromBody] EditSnsPostRequest req, CancellationToken ct)
    {
        var post = await _db.SocialPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post == null) return NotFound();

        if (req.Caption != null) post.Caption = req.Caption;
        if (req.ImageUrl != null) post.ImageUrl = req.ImageUrl;
        post.UpdatedBy = _currentUser.Username;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Post updated" });
    }

    /// <summary>Delete a pending post</summary>
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> DeletePost(int id, CancellationToken ct)
    {
        var post = await _db.SocialPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post == null) return NotFound();

        _db.SocialPosts.Remove(post);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Post deleted" });
    }

    /// <summary>Dashboard: SNS marketing stats</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var byPlatform = await _db.SocialPosts.AsNoTracking()
            .GroupBy(p => p.Platform)
            .Select(g => new { platform = g.Key, total = g.Count(), pending = g.Count(p => p.Status == "Pending"), posted = g.Count(p => p.Status == "Posted") })
            .ToListAsync(ct);

        var recentPosts = await _db.SocialPosts.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt).Take(10)
            .Select(p => new { p.Platform, p.Caption, p.Status, p.CreatedAt })
            .ToListAsync(ct);

        var totalPending = await _db.SocialPosts.CountAsync(p => p.Status == "Pending", ct);
        var totalPosted = await _db.SocialPosts.CountAsync(p => p.Status == "Posted", ct);

        return Ok(new
        {
            totalPending, totalPosted,
            byPlatform, recentPosts,
            platforms = new[] { "Instagram", "Facebook", "YouTube", "Twitter", "NaverBlog" }
        });
    }

    /// <summary>Generate AI-powered SNS content for a product</summary>
    [HttpPost("generate/{productId}")]
    public async Task<IActionResult> GenerateContent(int productId, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Tenant)
            .Include(p => p.Images.Take(1))
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null) return NotFound();

        var imageUrl = product.Images.FirstOrDefault()?.Url;

        // Trigger the marketing event handler
        // For now, create posts directly
        var platforms = new[] { "Instagram", "Facebook", "YouTube", "Twitter", "NaverBlog" };
        foreach (var platform in platforms)
        {
            _db.SocialPosts.Add(new Shop.Domain.Entities.SocialPost
            {
                TenantId = product.TenantId,
                ProductId = product.Id,
                Platform = platform,
                Caption = $"[{platform}] {product.Name} - {product.Tenant?.Name ?? "Shop"}\n\u20a9{product.Price:N0}\nSynDock Mall\uc5d0\uc11c \ub9cc\ub098\ubcf4\uc138\uc694!",
                ImageUrl = imageUrl,
                Status = "Pending",
                CreatedBy = "AI-Marketing-Manual"
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = $"Generated {platforms.Length} SNS posts for {product.Name}", platforms });
    }
}

public record EditSnsPostRequest(string? Caption = null, string? ImageUrl = null);
