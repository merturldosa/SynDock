namespace Shop.Application.Common.DTOs;

public record OrderDto(
    int Id,
    string OrderNumber,
    string Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal TotalAmount,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal PointsUsed,
    int? CouponId,
    string? Note,
    AddressDto? ShippingAddress,
    DateTime CreatedAt);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    int? VariantId,
    string? VariantName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record OrderSummaryDto(
    int Id,
    string OrderNumber,
    string Status,
    int ItemCount,
    decimal TotalAmount,
    string? FirstProductName,
    string? FirstProductImageUrl,
    DateTime CreatedAt);

public record AddressDto(
    int Id,
    string RecipientName,
    string Phone,
    string ZipCode,
    string Address1,
    string? Address2,
    bool IsDefault);
