using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Commands;

public record TenantThemeDto(string? Primary, string? PrimaryLight, string? Secondary, string? SecondaryLight, string? Background);

public record AiIntegrationDto(
    string? OpenAiApiKey, string? OpenAiModel,
    string? DalleModel, string? ClaudeApiKey, string? ClaudeModel,
    bool AiContentEnabled = false, bool AiImageEnabled = false);

public record UpdateTenantSettingsCommand(
    string? CompanyName, string? CompanyAddress, string? BusinessNumber,
    string? CeoName, string? ContactPhone, string? ContactEmail,
    string? HeroSubtitle, string? HeroTagline, string? HeroDescription,
    TenantThemeDto? Theme, string? LogoUrl, string? FaviconUrl,
    AiIntegrationDto? AiIntegration = null
) : IRequest<Result<bool>>;

public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateTenantSettingsCommandHandler> _logger;

    public UpdateTenantSettingsCommandHandler(IShopDbContext db, ITenantContext tenantContext, ILogger<UpdateTenantSettingsCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<bool>.Failure("Tenant information not found.");

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return Result<bool>.Failure("Tenant not found.");

        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse tenant ConfigJson"); config = new JsonObject(); }
        }

        // Update allowed fields only (never touch paymentConfig)
        if (request.CompanyName is not null) config["companyName"] = request.CompanyName;
        if (request.CompanyAddress is not null) config["companyAddress"] = request.CompanyAddress;
        if (request.BusinessNumber is not null) config["businessNumber"] = request.BusinessNumber;
        if (request.CeoName is not null) config["ceoName"] = request.CeoName;
        if (request.ContactPhone is not null) config["contactPhone"] = request.ContactPhone;
        if (request.ContactEmail is not null) config["contactEmail"] = request.ContactEmail;
        if (request.HeroSubtitle is not null) config["heroSubtitle"] = request.HeroSubtitle;
        if (request.HeroTagline is not null) config["heroTagline"] = request.HeroTagline;
        if (request.HeroDescription is not null) config["heroDescription"] = request.HeroDescription;
        if (request.LogoUrl is not null) config["logoUrl"] = request.LogoUrl;
        if (request.FaviconUrl is not null) config["faviconUrl"] = request.FaviconUrl;

        if (request.Theme is not null)
        {
            var themeObj = config["theme"]?.AsObject() ?? new JsonObject();
            if (request.Theme.Primary is not null) themeObj["primary"] = request.Theme.Primary;
            if (request.Theme.PrimaryLight is not null) themeObj["primaryLight"] = request.Theme.PrimaryLight;
            if (request.Theme.Secondary is not null) themeObj["secondary"] = request.Theme.Secondary;
            if (request.Theme.SecondaryLight is not null) themeObj["secondaryLight"] = request.Theme.SecondaryLight;
            if (request.Theme.Background is not null) themeObj["background"] = request.Theme.Background;
            config["theme"] = themeObj;
        }

        if (request.AiIntegration is not null)
        {
            var aiObj = config["aiIntegration"]?.AsObject() ?? new JsonObject();
            if (request.AiIntegration.OpenAiApiKey is not null) aiObj["openAiApiKey"] = request.AiIntegration.OpenAiApiKey;
            if (request.AiIntegration.OpenAiModel is not null) aiObj["openAiModel"] = request.AiIntegration.OpenAiModel;
            if (request.AiIntegration.DalleModel is not null) aiObj["dalleModel"] = request.AiIntegration.DalleModel;
            if (request.AiIntegration.ClaudeApiKey is not null) aiObj["claudeApiKey"] = request.AiIntegration.ClaudeApiKey;
            if (request.AiIntegration.ClaudeModel is not null) aiObj["claudeModel"] = request.AiIntegration.ClaudeModel;
            aiObj["aiContentEnabled"] = request.AiIntegration.AiContentEnabled;
            aiObj["aiImageEnabled"] = request.AiIntegration.AiImageEnabled;
            config["aiIntegration"] = aiObj;
        }

        tenant.ConfigJson = config.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        tenant.UpdatedBy = "TenantAdmin";
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
