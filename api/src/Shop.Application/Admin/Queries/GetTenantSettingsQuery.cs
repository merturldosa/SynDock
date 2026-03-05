using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record TenantSettingsDto(
    string? CompanyName, string? CompanyAddress, string? BusinessNumber,
    string? CeoName, string? ContactPhone, string? ContactEmail,
    string? HeroSubtitle, string? HeroTagline, string? HeroDescription,
    TenantSettingsThemeDto? Theme, string? LogoUrl, string? FaviconUrl);

public record TenantSettingsThemeDto(string? Primary, string? PrimaryLight, string? Secondary, string? SecondaryLight, string? Background);

public record GetTenantSettingsQuery : IRequest<Result<TenantSettingsDto>>;

public class GetTenantSettingsQueryHandler : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsDto>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetTenantSettingsQueryHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TenantSettingsDto>> Handle(GetTenantSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<TenantSettingsDto>.Failure("Tenant information not found.");

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return Result<TenantSettingsDto>.Failure("Tenant not found.");

        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch { /* ignore */ }
        }

        TenantSettingsThemeDto? theme = null;
        if (config["theme"] is JsonObject themeObj)
        {
            theme = new TenantSettingsThemeDto(
                themeObj["primary"]?.GetValue<string>(),
                themeObj["primaryLight"]?.GetValue<string>(),
                themeObj["secondary"]?.GetValue<string>(),
                themeObj["secondaryLight"]?.GetValue<string>(),
                themeObj["background"]?.GetValue<string>());
        }

        var dto = new TenantSettingsDto(
            config["companyName"]?.GetValue<string>(),
            config["companyAddress"]?.GetValue<string>(),
            config["businessNumber"]?.GetValue<string>(),
            config["ceoName"]?.GetValue<string>(),
            config["contactPhone"]?.GetValue<string>(),
            config["contactEmail"]?.GetValue<string>(),
            config["heroSubtitle"]?.GetValue<string>(),
            config["heroTagline"]?.GetValue<string>(),
            config["heroDescription"]?.GetValue<string>(),
            theme,
            config["logoUrl"]?.GetValue<string>(),
            config["faviconUrl"]?.GetValue<string>());

        return Result<TenantSettingsDto>.Success(dto);
    }
}
