namespace Shop.Application.Common.DTOs;

public record CouponDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsageCount,
    int CurrentUsageCount,
    bool IsActive,
    DateTime CreatedAt);

public record UserCouponDto(
    int Id,
    int CouponId,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime EndDate,
    bool IsUsed,
    DateTime? UsedAt);

public record CouponValidationResult(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountAmount);

public record PagedCoupons(
    IReadOnlyList<CouponDto> Items,
    int TotalCount);
