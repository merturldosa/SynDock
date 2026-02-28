namespace Shop.Application.Common.DTOs;

public record CartDto(
    int Id,
    IReadOnlyList<CartItemDto> Items,
    decimal TotalAmount,
    int TotalQuantity);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    decimal Price,
    decimal? SalePrice,
    string PriceType,
    int? VariantId,
    string? VariantName,
    int Quantity,
    decimal SubTotal);
