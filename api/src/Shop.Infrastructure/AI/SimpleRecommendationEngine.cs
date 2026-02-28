using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.AI;

public class SimpleRecommendationEngine : IRecommendationEngine
{
    private readonly IShopDbContext _db;

    public SimpleRecommendationEngine(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForProductAsync(
        int productId,
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        // Get the product's category
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
            return [];

        // Recommend products from same category, excluding the current product
        var sameCategoryProducts = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id,
                p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price,
                p.SalePrice,
                CalculateScore(p.ViewCount, p.IsFeatured)))
            .ToListAsync(cancellationToken);

        // If not enough, fill with popular products from other categories
        if (sameCategoryProducts.Count < count)
        {
            var remaining = count - sameCategoryProducts.Count;
            var excludeIds = sameCategoryProducts.Select(p => p.ProductId).Append(productId).ToList();

            var additionalProducts = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => !excludeIds.Contains(p.Id) && p.IsActive)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.ViewCount)
                .Take(remaining)
                .Select(p => new RecommendedProduct(
                    p.Id,
                    p.Name,
                    p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                    p.Price,
                    p.SalePrice,
                    CalculateScore(p.ViewCount, p.IsFeatured) * 0.8))
                .ToListAsync(cancellationToken);

            sameCategoryProducts.AddRange(additionalProducts);
        }

        return sameCategoryProducts;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForUserAsync(
        int userId,
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        // Get categories the user has ordered from
        var userCategoryIds = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.UserId == userId)
            .Select(oi => oi.Product.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userCategoryIds.Count == 0)
            return await GetPopularProductsAsync(count, cancellationToken);

        // Get previously ordered product IDs to exclude
        var orderedProductIds = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.UserId == userId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Recommend from user's preferred categories
        var recommendations = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => userCategoryIds.Contains(p.CategoryId) && !orderedProductIds.Contains(p.Id) && p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id,
                p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price,
                p.SalePrice,
                CalculateScore(p.ViewCount, p.IsFeatured)))
            .ToListAsync(cancellationToken);

        // Fill with popular if not enough
        if (recommendations.Count < count)
        {
            var remaining = count - recommendations.Count;
            var excludeIds = recommendations.Select(p => p.ProductId).Concat(orderedProductIds).ToList();

            var popular = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => !excludeIds.Contains(p.Id) && p.IsActive)
                .OrderByDescending(p => p.ViewCount)
                .Take(remaining)
                .Select(p => new RecommendedProduct(
                    p.Id,
                    p.Name,
                    p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                    p.Price,
                    p.SalePrice,
                    CalculateScore(p.ViewCount, p.IsFeatured) * 0.7))
                .ToListAsync(cancellationToken);

            recommendations.AddRange(popular);
        }

        return recommendations;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetPopularProductsAsync(
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id,
                p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price,
                p.SalePrice,
                CalculateScore(p.ViewCount, p.IsFeatured)))
            .ToListAsync(cancellationToken);
    }

    private static double CalculateScore(int viewCount, bool isFeatured)
    {
        var score = Math.Min(viewCount / 100.0, 1.0);
        if (isFeatured) score = Math.Min(score + 0.3, 1.0);
        return Math.Round(score, 2);
    }
}
