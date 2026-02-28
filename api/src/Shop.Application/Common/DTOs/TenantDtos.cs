namespace Shop.Application.Common.DTOs;

public record TenantDto(int Id, string Slug, string Name, string? CustomDomain, string? Subdomain, bool IsActive, string? ConfigJson);

public record TenantInfoDto(string Slug, string Name, string? ConfigJson);
