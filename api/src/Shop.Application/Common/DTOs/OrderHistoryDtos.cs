namespace Shop.Application.Common.DTOs;

public record OrderHistoryDto(
    int Id,
    string Status,
    string? Note,
    string? TrackingNumber,
    string? TrackingCarrier,
    string CreatedBy,
    DateTime CreatedAt);
