namespace Shop.Application.Common.DTOs;

public record BaptismalNameDto(
    string? BaptismalName,
    int? PatronSaintId,
    SaintSummaryDto? PatronSaint);

public record UpdateBaptismalNameRequest(string BaptismalName);
