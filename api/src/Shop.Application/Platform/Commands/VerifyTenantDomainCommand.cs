using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record DomainVerificationResultDto(bool IsVerified, string Message);

public record VerifyTenantDomainCommand(int TenantId) : IRequest<Result<DomainVerificationResultDto>>;

public class VerifyTenantDomainCommandHandler : IRequestHandler<VerifyTenantDomainCommand, Result<DomainVerificationResultDto>>
{
    private readonly IShopDbContext _db;

    public VerifyTenantDomainCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DomainVerificationResultDto>> Handle(VerifyTenantDomainCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        if (tenant is null)
            return Result<DomainVerificationResultDto>.Failure("테넌트를 찾을 수 없습니다.");

        if (string.IsNullOrEmpty(tenant.CustomDomain))
            return Result<DomainVerificationResultDto>.Success(new DomainVerificationResultDto(false, "커스텀 도메인이 설정되지 않았습니다."));

        // DNS lookup
        var isVerified = false;
        var message = "DNS 레코드를 확인할 수 없습니다. CNAME 설정을 확인해 주세요.";

        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(tenant.CustomDomain, cancellationToken);
            if (hostEntry.AddressList.Length > 0)
            {
                isVerified = true;
                message = "도메인이 성공적으로 확인되었습니다.";
            }
        }
        catch (Exception)
        {
            // DNS lookup failed - domain not yet configured
        }

        // Update config
        var config = new JsonObject();
        if (!string.IsNullOrEmpty(tenant.ConfigJson))
        {
            try { config = JsonNode.Parse(tenant.ConfigJson)?.AsObject() ?? new JsonObject(); }
            catch { config = new JsonObject(); }
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
