namespace Shop.Application.Common.Interfaces;

public record RecommendedProduct(int ProductId, string ProductName, string? ImageUrl, decimal Price, decimal? SalePrice, double Score);

public interface IRecommendationEngine
{
    Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForProductAsync(
        int productId,
        int count = 6,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendedProduct>> GetRecommendationsForUserAsync(
        int userId,
        int count = 6,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendedProduct>> GetPopularProductsAsync(
        int count = 6,
        CancellationToken cancellationToken = default);
}
