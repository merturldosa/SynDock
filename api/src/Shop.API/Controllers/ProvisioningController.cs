using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
public class ProvisioningController : ControllerBase
{
    private readonly IProvisioningService _provisioning;
    private readonly ICurrentUserService _currentUser;

    public ProvisioningController(IProvisioningService provisioning, ICurrentUserService currentUser)
    {
        _provisioning = provisioning;
        _currentUser = currentUser;
    }

    /// <summary>공개: 분양 신청 제출</summary>
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<IActionResult> Apply([FromBody] TenantApplicationRequest req, CancellationToken ct)
    {
        var result = await _provisioning.SubmitApplicationAsync(
            req.CompanyName, req.DesiredSlug, req.Email, req.Phone, req.ContactName,
            req.BusinessType, req.PlanTier, req.BusinessDescription, req.WebsiteUrl,
            req.NeedsMes, req.NeedsWms, req.NeedsErp, req.NeedsCrm, req.AdditionalInfoJson, ct);
        return Ok(new { message = "신청이 접수되었습니다", applicationId = result.Id });
    }

    /// <summary>공개: 슬러그 사용 가능 여부 확인</summary>
    [HttpGet("check-slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckSlug(string slug, CancellationToken ct)
        => Ok(new { slug, available = await _provisioning.IsSlugAvailableAsync(slug, ct) });

    /// <summary>관리자: 신청 목록 조회</summary>
    [HttpGet("applications")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> GetApplications([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, totalCount) = await _provisioning.GetApplicationsAsync(status, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    /// <summary>관리자: 신청 상세 조회</summary>
    [HttpGet("applications/{id}")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> GetApplication(int id, CancellationToken ct)
    {
        var app = await _provisioning.GetApplicationAsync(id, ct);
        return app == null ? NotFound() : Ok(app);
    }

    /// <summary>관리자: 신청 승인</summary>
    [HttpPost("applications/{id}/approve")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveApplicationRequest req, CancellationToken ct)
    {
        await _provisioning.ApproveApplicationAsync(id, req.AdminNotes ?? "", _currentUser.Username ?? "system", ct);
        return Ok(new { message = "신청이 승인되었습니다" });
    }

    /// <summary>관리자: 신청 거절</summary>
    [HttpPost("applications/{id}/reject")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectApplicationRequest req, CancellationToken ct)
    {
        await _provisioning.RejectApplicationAsync(id, req.RejectionReason, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "신청이 거절되었습니다" });
    }

    /// <summary>관리자: 테넌트 프로비저닝 실행</summary>
    [HttpPost("applications/{id}/provision")]
    [Authorize(Roles = "PlatformAdmin")]
    public async Task<IActionResult> Provision(int id, CancellationToken ct)
    {
        var tenant = await _provisioning.ProvisionTenantAsync(id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "테넌트가 성공적으로 프로비저닝되었습니다", tenantId = tenant.Id, slug = tenant.Slug });
    }

    /// <summary>공개: 신청 상태 조회 (이메일 + 신청번호)</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckStatus([FromQuery] string email, [FromQuery] int applicationId, CancellationToken ct)
    {
        var app = await _provisioning.GetApplicationAsync(applicationId, ct);
        if (app == null || !string.Equals(app.Email, email, StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = "신청 정보를 찾을 수 없습니다. 이메일과 신청번호를 확인해주세요." });

        return Ok(new
        {
            applicationId = app.Id,
            companyName = app.CompanyName,
            desiredSlug = app.DesiredSlug,
            status = app.Status,
            statusLabel = app.Status switch
            {
                "Pending" => "검토 대기중",
                "Reviewing" => "검토 진행중",
                "Approved" => "승인 완료 (프로비저닝 대기)",
                "Provisioning" => "쇼핑몰 생성중",
                "Active" => "개설 완료!",
                "Rejected" => "신청 거절",
                _ => app.Status
            },
            planTier = app.PlanTier,
            createdAt = app.CreatedAt,
            approvedAt = app.ApprovedAt,
            provisionedAt = app.ProvisionedAt,
            rejectedAt = app.RejectedAt,
            rejectionReason = app.RejectionReason,
            shopUrl = app.Status == "Active" ? $"https://{app.DesiredSlug}.syndock.com" : null
        });
    }
}

// Request DTOs
public class TenantApplicationRequest
{
    public string CompanyName { get; set; } = "";
    public string DesiredSlug { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string ContactName { get; set; } = "";
    public string BusinessType { get; set; } = "Retail";
    public string PlanTier { get; set; } = "Starter";
    public string? BusinessDescription { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool NeedsMes { get; set; }
    public bool NeedsWms { get; set; }
    public bool NeedsErp { get; set; }
    public bool NeedsCrm { get; set; }
    public string? AdditionalInfoJson { get; set; }
}
public record ApproveApplicationRequest(string? AdminNotes = null);
public record RejectApplicationRequest(string RejectionReason);
