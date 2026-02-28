namespace Shop.Application.Common.DTOs;

public record WishlistItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    decimal Price,
    decimal? SalePrice,
    string PriceType,
    DateTime CreatedAt);
