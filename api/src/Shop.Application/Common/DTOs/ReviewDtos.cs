namespace Shop.Application.Common.DTOs;

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    int Rating,
    string? Content,
    bool IsVisible,
    DateTime CreatedAt);

public record ReviewSummaryDto(
    int TotalCount,
    double AverageRating,
    IReadOnlyList<ReviewDto> Reviews);
