using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.AI;

/// <summary>
/// Item-Item collaborative filtering based on co-purchase patterns.
/// Falls back to category-based + popularity when insufficient data.
/// </summary>
public class CollaborativeFilteringEngine : IRecommendationEngine
{
    private readonly IShopDbContext _db;

    public CollaborativeFilteringEngine(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForProductAsync(
        int productId, int count = 6, CancellationToken ct = default)
    {
        // 1. Find users who purchased this product
        var buyerIds = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.Order.UserId)
            .Distinct()
            .ToListAsync(ct);

        List<RecommendedProduct> results;

        if (buyerIds.Count >= 2)
        {
            // 2. Find co-purchased products and count frequency
            var coPurchased = await _db.OrderItems.AsNoTracking()
                .Include(oi => oi.Product).ThenInclude(p => p.Images)
                .Where(oi => buyerIds.Contains(oi.Order.UserId)
                             && oi.ProductId != productId
                             && oi.Product.IsActive)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    CoCount = g.Select(x => x.Order.UserId).Distinct().Count(),
                    Product = g.First().Product
                })
                .OrderByDescending(x => x.CoCount)
                .Take(count)
                .ToListAsync(ct);

            var maxCo = coPurchased.Count > 0 ? coPurchased.Max(x => x.CoCount) : 1;

            results = coPurchased.Select(x => new RecommendedProduct(
                x.Product.Id,
                x.Product.Name,
                x.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                x.Product.Price,
                x.Product.SalePrice,
                Math.Round((double)x.CoCount / maxCo, 2)
            )).ToList();
        }
        else
        {
            // Fallback: category-based similarity
            results = await GetCategoryBasedRecommendations(productId, count, ct);
        }

        // Fill remaining with popular
        if (results.Count < count)
        {
            var excludeIds = results.Select(r => r.ProductId).Append(productId).ToHashSet();
            var popular = await GetPopularExcluding(excludeIds, count - results.Count, ct);
            results.AddRange(popular);
        }

        return results;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForUserAsync(
        int userId, int count = 6, CancellationToken ct = default)
    {
        // Get user's purchased products
        var purchasedIds = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.UserId == userId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync(ct);

        if (purchasedIds.Count == 0)
            return await GetPopularProductsAsync(count, ct);

        // Find users who bought the same products (collaborative neighbors)
        var neighborIds = await _db.OrderItems.AsNoTracking()
            .Where(oi => purchasedIds.Contains(oi.ProductId) && oi.Order.UserId != userId)
            .Select(oi => oi.Order.UserId)
            .Distinct()
            .Take(50) // limit neighbors
            .ToListAsync(ct);

        if (neighborIds.Count == 0)
        {
            // Fallback to category-based
            var categoryIds = await _db.OrderItems.AsNoTracking()
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.UserId == userId)
                .Select(oi => oi.Product.CategoryId)
                .Distinct()
                .ToListAsync(ct);

            return await GetFromCategories(categoryIds, purchasedIds.ToHashSet(), count, ct);
        }

        // Get products bought by neighbors but not by this user
        var recommendations = await _db.OrderItems.AsNoTracking()
            .Include(oi => oi.Product).ThenInclude(p => p.Images)
            .Where(oi => neighborIds.Contains(oi.Order.UserId)
                         && !purchasedIds.Contains(oi.ProductId)
                         && oi.Product.IsActive)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                NeighborCount = g.Select(x => x.Order.UserId).Distinct().Count(),
                Product = g.First().Product
            })
            .OrderByDescending(x => x.NeighborCount)
            .Take(count)
            .ToListAsync(ct);

        var maxN = recommendations.Count > 0 ? recommendations.Max(x => x.NeighborCount) : 1;

        var results = recommendations.Select(x => new RecommendedProduct(
            x.Product.Id,
            x.Product.Name,
            x.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            x.Product.Price,
            x.Product.SalePrice,
            Math.Round((double)x.NeighborCount / maxN, 2)
        )).ToList();

        if (results.Count < count)
        {
            var excludeIds = results.Select(r => r.ProductId).Concat(purchasedIds).ToHashSet();
            var popular = await GetPopularExcluding(excludeIds, count - results.Count, ct);
            results.AddRange(popular);
        }

        return results;
    }

    public async Task<IReadOnlyList<RecommendedProduct>> GetPopularProductsAsync(
        int count = 6, CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id, p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price, p.SalePrice,
                Math.Round(Math.Min(p.ViewCount / 100.0, 1.0) + (p.IsFeatured ? 0.3 : 0), 2)))
            .ToListAsync(ct);
    }

    private async Task<List<RecommendedProduct>> GetCategoryBasedRecommendations(
        int productId, int count, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null) return [];

        return await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id, p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price, p.SalePrice,
                Math.Round(Math.Min(p.ViewCount / 100.0, 1.0) + (p.IsFeatured ? 0.3 : 0), 2)))
            .ToListAsync(ct);
    }

    private async Task<List<RecommendedProduct>> GetFromCategories(
        List<int> categoryIds, HashSet<int> excludeIds, int count, CancellationToken ct)
    {
        return await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => categoryIds.Contains(p.CategoryId) && !excludeIds.Contains(p.Id) && p.IsActive)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id, p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price, p.SalePrice,
                Math.Round(Math.Min(p.ViewCount / 100.0, 1.0) + (p.IsFeatured ? 0.3 : 0), 2)))
            .ToListAsync(ct);
    }

    private async Task<List<RecommendedProduct>> GetPopularExcluding(
        HashSet<int> excludeIds, int count, CancellationToken ct)
    {
        return await _db.Products.AsNoTracking()
            .Include(p => p.Images)
            .Where(p => !excludeIds.Contains(p.Id) && p.IsActive)
            .OrderByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => new RecommendedProduct(
                p.Id, p.Name,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.Price, p.SalePrice,
                Math.Round(Math.Min(p.ViewCount / 100.0, 1.0) * 0.7, 2)))
            .ToListAsync(ct);
    }
}
