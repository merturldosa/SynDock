namespace Shop.Application.Common.DTOs;

public record NotificationDto(
    int Id,
    string Type,
    string Title,
    string? Message,
    bool IsRead,
    DateTime? ReadAt,
    int? ReferenceId,
    string? ReferenceType,
    DateTime CreatedAt);

public record PagedNotifications(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount);

public record UnreadCountDto(
    int Count);
