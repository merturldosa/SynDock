using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record TenantSettingsDto(
    string? CompanyName, string? CompanyAddress, string? BusinessNumber,
    string? CeoName, string? ContactPhone, string? ContactEmail,
    string? HeroSubtitle, string? HeroTagline, string? HeroDescription,
    TenantSettingsThemeDto? Theme, string? LogoUrl, string? FaviconUrl,
    TenantAiIntegrationDto? AiIntegration = null);

public record TenantSettingsThemeDto(string? Primary, string? PrimaryLight, string? Secondary, string? SecondaryLight, string? Background);

public record TenantAiIntegrationDto(
    string? OpenAiApiKey, string? OpenAiModel, string? DalleModel,
    string? ClaudeApiKey, string? ClaudeModel,
    bool AiContentEnabled, bool AiImageEnabled);

public record GetTenantSettingsQuery : IRequest<Result<TenantSettingsDto>>;

public class GetTenantSettingsQueryHandler : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsDto>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetTenantSettingsQueryHandler> _logger;

    public GetTenantSettingsQueryHandler(IShopDbContext db, ITenantContext tenantContext, ILogger<GetTenantSettingsQueryHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
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
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse tenant ConfigJson"); }
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

        TenantAiIntegrationDto? aiIntegration = null;
        if (config["aiIntegration"] is JsonObject aiObj)
        {
            aiIntegration = new TenantAiIntegrationDto(
                aiObj["openAiApiKey"]?.GetValue<string>(),
                aiObj["openAiModel"]?.GetValue<string>(),
                aiObj["dalleModel"]?.GetValue<string>(),
                aiObj["claudeApiKey"]?.GetValue<string>(),
                aiObj["claudeModel"]?.GetValue<string>(),
                aiObj["aiContentEnabled"]?.GetValue<bool>() ?? false,
                aiObj["aiImageEnabled"]?.GetValue<bool>() ?? false);
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
            config["faviconUrl"]?.GetValue<string>(),
            aiIntegration);

        return Result<TenantSettingsDto>.Success(dto);
    }
}
