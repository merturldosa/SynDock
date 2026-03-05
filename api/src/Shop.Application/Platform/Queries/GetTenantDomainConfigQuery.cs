using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Platform.Commands;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Queries;

public record GetTenantDomainConfigQuery(string Slug) : IRequest<Result<DomainConfigDto>>;

public class GetTenantDomainConfigQueryHandler : IRequestHandler<GetTenantDomainConfigQuery, Result<DomainConfigDto>>
{
    private readonly IShopDbContext _db;

    public GetTenantDomainConfigQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DomainConfigDto>> Handle(GetTenantDomainConfigQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == request.Slug, cancellationToken);
        if (tenant is null)
            return Result<DomainConfigDto>.Failure("Tenant not found.");

        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch { /* ignore */ }
        }

        var domainConfig = config["domainConfig"]?.AsObject();
        var verificationStatus = domainConfig?["verificationStatus"]?.GetValue<string>() ?? "None";
        DateTime? verifiedAt = null;
        if (domainConfig?["verifiedAt"] is not null)
        {
            try { verifiedAt = DateTime.Parse(domainConfig["verifiedAt"]!.GetValue<string>()); }
            catch { /* ignore */ }
        }
        var sslStatus = domainConfig?["sslStatus"]?.GetValue<string>() ?? "None";
        DateTime? sslExpiresAt = null;
        if (domainConfig?["sslExpiresAt"] is not null)
        {
            try { sslExpiresAt = DateTime.Parse(domainConfig["sslExpiresAt"]!.GetValue<string>()); }
            catch { /* ignore */ }
        }

        var dnsInstructions = new List<DnsInstructionDto>();
        if (!string.IsNullOrEmpty(tenant.CustomDomain))
        {
            dnsInstructions.Add(new DnsInstructionDto("CNAME", tenant.CustomDomain, "shop.syndock.com"));
            dnsInstructions.Add(new DnsInstructionDto("TXT", $"_syndock.{tenant.CustomDomain}", "syndock-verify=pending"));
        }

        return Result<DomainConfigDto>.Success(new DomainConfigDto(
            tenant.CustomDomain, tenant.Subdomain,
            verificationStatus, verifiedAt, sslStatus, sslExpiresAt, dnsInstructions));
    }
}
