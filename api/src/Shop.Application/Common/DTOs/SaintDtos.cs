namespace Shop.Application.Common.DTOs;

public record SaintDto(
    int Id,
    string KoreanName,
    string? LatinName,
    string? EnglishName,
    string? Description,
    DateTime? FeastDay,
    string? Patronage,
    bool IsActive);

public record SaintSummaryDto(
    int Id,
    string KoreanName,
    string? LatinName,
    DateTime? FeastDay,
    string? Patronage);
