using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record DomainVerificationResultDto(bool IsVerified, string Message);

public record VerifyTenantDomainCommand(int TenantId) : IRequest<Result<DomainVerificationResultDto>>;

public class VerifyTenantDomainCommandHandler : IRequestHandler<VerifyTenantDomainCommand, Result<DomainVerificationResultDto>>
{
    private readonly IShopDbContext _db;
    private readonly ILogger<VerifyTenantDomainCommandHandler> _logger;

    public VerifyTenantDomainCommandHandler(IShopDbContext db, ILogger<VerifyTenantDomainCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<DomainVerificationResultDto>> Handle(VerifyTenantDomainCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        if (tenant is null)
            return Result<DomainVerificationResultDto>.Failure("Tenant not found.");

        if (string.IsNullOrEmpty(tenant.CustomDomain))
            return Result<DomainVerificationResultDto>.Success(new DomainVerificationResultDto(false, "Custom domain is not configured."));

        // DNS lookup
        var isVerified = false;
        var message = "DNS record not found. Please verify your CNAME configuration.";

        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(tenant.CustomDomain, cancellationToken);
            if (hostEntry.AddressList.Length > 0)
            {
                isVerified = true;
                message = "Domain verified successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DNS lookup failed for {Domain}", tenant.CustomDomain);
        }

        // Update config
        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to parse tenant ConfigJson"); config = new JsonObject(); }
        }

        var domainConfig = config["domainConfig"]?.AsObject() ?? new JsonObject();
        domainConfig["verificationStatus"] = isVerified ? "Verified" : "Failed";
        if (isVerified)
        {
            domainConfig["verifiedAt"] = DateTime.UtcNow.ToString("O");
            domainConfig["sslStatus"] = "Active";
        }
        config["domainConfig"] = domainConfig;

        tenant.ConfigJson = config.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<DomainVerificationResultDto>.Success(new DomainVerificationResultDto(isVerified, message));
    }
}
