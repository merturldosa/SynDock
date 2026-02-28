namespace Shop.Application.Common.DTOs;

public record ProductListDto(
    int Id, string Name, string? Slug, decimal Price, decimal? SalePrice,
    string PriceType, string? Specification, int CategoryId, string CategoryName,
    string? PrimaryImageUrl, bool IsFeatured, bool IsNew, int ViewCount);

public record ProductDetailDto(
    int Id, string Name, string? Slug, string? Description,
    decimal Price, decimal? SalePrice, string PriceType, string? Specification,
    int CategoryId, string CategoryName, bool IsFeatured, bool IsNew, int ViewCount,
    string? CustomFieldsJson,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductVariantDto> Variants);

public record ProductImageDto(int Id, string Url, string? AltText, int SortOrder, bool IsPrimary);

public record ProductVariantDto(int Id, string Name, string? Sku, decimal? Price, int Stock, bool IsActive);
