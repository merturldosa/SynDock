namespace Shop.Application.Common.DTOs;

public record PointBalanceDto(
    decimal Balance);

public record PointHistoryDto(
    int Id,
    decimal Amount,
    string TransactionType,
    string? Description,
    int? OrderId,
    DateTime CreatedAt);

public record PagedPointHistory(
    IReadOnlyList<PointHistoryDto> Items,
    int TotalCount);
