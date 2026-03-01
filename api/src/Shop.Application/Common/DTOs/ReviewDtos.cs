namespace Shop.Application.Common.DTOs;

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    int Rating,
    string? Content,
    string? ImageUrl,
    bool IsVisible,
    DateTime CreatedAt);

public record RatingDistributionDto(int Rating, int Count);

public record ReviewSummaryDto(
    int TotalCount,
    double AverageRating,
    int PhotoReviewCount,
    IReadOnlyList<RatingDistributionDto> RatingDistribution,
    IReadOnlyList<ReviewDto> Reviews);
