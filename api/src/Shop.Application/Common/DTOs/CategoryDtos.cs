namespace Shop.Application.Common.DTOs;

public record CategoryDto(int Id, string Name, string? Slug, string? Description, string? Icon, int? ParentId, int SortOrder, int ProductCount);

public record CategoryTreeDto(int Id, string Name, string? Slug, string? Icon, int SortOrder, int ProductCount, IReadOnlyList<CategoryTreeDto> Children);
