using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Commands;

public record AutoPostProductCommand(int ProductId) : IRequest<Result<List<SocialPostResultDto>>>;

public record SocialPostResultDto(string Platform, bool Success, string? PostUrl, string? Error);

public class AutoPostProductCommandHandler : IRequestHandler<AutoPostProductCommand, Result<List<SocialPostResultDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ISocialMediaService _social;
    private readonly ITenantContext _tenantContext;

    public AutoPostProductCommandHandler(IShopDbContext db, ISocialMediaService social, ITenantContext tenantContext)
    {
        _db = db;
        _social = social;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<SocialPostResultDto>>> Handle(AutoPostProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            return Result<List<SocialPostResultDto>>.Failure("Product not found.");

        var imageUrl = product.Images?.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;
        var caption = BuildCaption(product);
        var results = new List<SocialPostResultDto>();

        // Instagram
        if (await _social.IsConfiguredAsync("Instagram", cancellationToken) && !string.IsNullOrEmpty(imageUrl))
        {
            var igResult = await _social.PostToInstagramAsync(caption, imageUrl, cancellationToken);
            await SaveSocialPost("Instagram", product.Id, caption, imageUrl, igResult, cancellationToken);
            results.Add(new SocialPostResultDto("Instagram", igResult.Success, igResult.PostUrl, igResult.ErrorMessage));
        }

        // Facebook
        if (await _social.IsConfiguredAsync("Facebook", cancellationToken))
        {
            var fbResult = await _social.PostToFacebookAsync(caption, imageUrl, cancellationToken);
            await SaveSocialPost("Facebook", product.Id, caption, imageUrl, fbResult, cancellationToken);
            results.Add(new SocialPostResultDto("Facebook", fbResult.Success, fbResult.PostUrl, fbResult.ErrorMessage));
        }

        return Result<List<SocialPostResultDto>>.Success(results);
    }

    private async Task SaveSocialPost(string platform, int productId, string caption, string? imageUrl, SocialPostResult result, CancellationToken ct)
    {
        var post = new SocialPost
        {
            TenantId = _tenantContext.TenantId,
            ProductId = productId,
            Platform = platform,
            Status = result.Success ? "Posted" : "Failed",
            Caption = caption,
            ImageUrl = imageUrl,
            PostUrl = result.PostUrl,
            ExternalPostId = result.PostId,
            ErrorMessage = result.ErrorMessage,
            PostedAt = result.Success ? DateTime.UtcNow : null,
            CreatedBy = "System"
        };

        await _db.SocialPosts.AddAsync(post, ct);
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildCaption(Product product)
    {
        var lines = new List<string>();
        lines.Add($"[신상품] {product.Name}");

        if (!string.IsNullOrEmpty(product.Specification))
            lines.Add(product.Specification);

        if (product.SalePrice.HasValue && product.SalePrice < product.Price)
        {
            var discount = Math.Round((1 - (double)product.SalePrice.Value / (double)product.Price) * 100);
            lines.Add($"{discount}% OFF! {product.SalePrice.Value:N0}원 (정가 {product.Price:N0}원)");
        }
        else if (product.PriceType != "Inquiry")
        {
            lines.Add($"{product.Price:N0}원");
        }

        lines.Add("");
        lines.Add("#신상품 #쇼핑 #추천");

        return string.Join("\n", lines);
    }
}

public record GetSocialPostsQuery(int? ProductId = null, string? Platform = null) : IRequest<Result<List<SocialPostDto>>>;

public record SocialPostDto(
    int Id, int? ProductId, string? ProductName, string Platform,
    string Status, string Caption, string? ImageUrl, string? PostUrl,
    string? ErrorMessage, DateTime? PostedAt, DateTime CreatedAt);

public class GetSocialPostsQueryHandler : IRequestHandler<GetSocialPostsQuery, Result<List<SocialPostDto>>>
{
    private readonly IShopDbContext _db;

    public GetSocialPostsQueryHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<SocialPostDto>>> Handle(GetSocialPostsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.SocialPosts.AsNoTracking()
            .Include(s => s.Product)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (request.ProductId.HasValue)
            query = query.Where(s => s.ProductId == request.ProductId.Value);
        if (!string.IsNullOrEmpty(request.Platform))
            query = query.Where(s => s.Platform == request.Platform);

        var posts = await query
            .Select(s => new SocialPostDto(
                s.Id, s.ProductId, s.Product != null ? s.Product.Name : null,
                s.Platform, s.Status, s.Caption, s.ImageUrl, s.PostUrl,
                s.ErrorMessage, s.PostedAt, s.CreatedAt))
            .Take(50)
            .ToListAsync(cancellationToken);

        return Result<List<SocialPostDto>>.Success(posts);
    }
}
