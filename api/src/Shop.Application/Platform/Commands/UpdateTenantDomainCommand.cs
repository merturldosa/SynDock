using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record DomainConfigDto(
    string? CustomDomain, string? Subdomain,
    string VerificationStatus, DateTime? VerifiedAt,
    string SslStatus, DateTime? SslExpiresAt,
    IReadOnlyList<DnsInstructionDto> DnsInstructions);

public record DnsInstructionDto(string Type, string Host, string Target);

public record UpdateTenantDomainCommand(int TenantId, string? CustomDomain, string? Subdomain) : IRequest<Result<DomainConfigDto>>;

public class UpdateTenantDomainCommandHandler : IRequestHandler<UpdateTenantDomainCommand, Result<DomainConfigDto>>
{
    private readonly IShopDbContext _db;

    public UpdateTenantDomainCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DomainConfigDto>> Handle(UpdateTenantDomainCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        if (tenant is null)
            return Result<DomainConfigDto>.Failure("Tenant not found.");

        if (request.CustomDomain is not null)
            tenant.CustomDomain = string.IsNullOrWhiteSpace(request.CustomDomain) ? null : request.CustomDomain;
        if (request.Subdomain is not null)
            tenant.Subdomain = string.IsNullOrWhiteSpace(request.Subdomain) ? null : request.Subdomain;

        // Store domain config in ConfigJson
        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch { config = new JsonObject(); }
        }

        var domainConfig = new JsonObject
        {
            ["verificationStatus"] = "Pending",
            ["verifiedAt"] = (JsonNode?)null,
            ["sslStatus"] = "Pending",
            ["sslExpiresAt"] = (JsonNode?)null,
            ["updatedAt"] = DateTime.UtcNow.ToString("O")
        };
        config["domainConfig"] = domainConfig;

        tenant.ConfigJson = config.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        tenant.UpdatedBy = "PlatformAdmin";
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var dnsInstructions = BuildDnsInstructions(tenant.CustomDomain);

        return Result<DomainConfigDto>.Success(new DomainConfigDto(
            tenant.CustomDomain, tenant.Subdomain,
            "Pending", null, "Pending", null, dnsInstructions));
    }

    private static List<DnsInstructionDto> BuildDnsInstructions(string? customDomain)
    {
        if (string.IsNullOrEmpty(customDomain))
            return [];

        return [
            new DnsInstructionDto("CNAME", customDomain, "shop.syndock.com"),
            new DnsInstructionDto("TXT", $"_syndock.{customDomain}", "syndock-verify=pending")
        ];
    }
}
