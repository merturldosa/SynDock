namespace Shop.Application.Common.Interfaces;

public interface IShopMigrationService
{
    Task<MigrationJobDto> StartMigrationAsync(int? tenantId, int? applicationId, string sourceUrl, string sourceType, string createdBy, CancellationToken ct = default);
    Task<MigrationJobDto?> GetJobAsync(int jobId, CancellationToken ct = default);
    Task<List<MigrationJobDto>> GetJobsAsync(int? tenantId = null, CancellationToken ct = default);
    Task<MigrationPreviewDto> PreviewMigrationAsync(string sourceUrl, CancellationToken ct = default);
    Task ImportCrawlResultsAsync(int jobId, int tenantId, CancellationToken ct = default);
}

public record MigrationJobDto(
    int Id, string SourceUrl, string SourceType, string Status,
    int TotalProductsFound, int TotalCategoriesFound, int TotalImagesFound,
    int ProductsImported, int CategoriesImported, int ImagesImported,
    int FailedItems, decimal ProgressPercent,
    DateTime? StartedAt, DateTime? CompletedAt, string? ErrorMessage);

public record MigrationPreviewDto(
    string SourceUrl, string SourceType, string ShopName,
    List<CrawledCategory> Categories, List<CrawledProduct> Products,
    int TotalImages, string? LogoUrl);

public record CrawledCategory(string Name, string? ParentName, int ProductCount);

public record CrawledProduct(
    string Name, string? Description, decimal Price, decimal? SalePrice,
    string CategoryName, string? ImageUrl, string? Sku,
    Dictionary<string, string>? Attributes);
