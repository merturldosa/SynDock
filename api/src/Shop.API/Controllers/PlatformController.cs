using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Platform.Commands;
using Shop.Application.Platform.Queries;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Infrastructure.Data;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformController : ControllerBase
{
    private readonly ShopDbContext _db;
    private readonly IMediator _mediator;

    public PlatformController(ShopDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new TenantDto(t.Id, t.Slug, t.Name, t.CustomDomain, t.Subdomain, t.IsActive, t.ConfigJson))
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetTenant(string slug)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        return Ok(new TenantDto(tenant.Id, tenant.Slug, tenant.Name, tenant.CustomDomain, tenant.Subdomain, tenant.IsActive, tenant.ConfigJson));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (await _db.Tenants.AnyAsync(t => t.Slug == request.Slug))
            return BadRequest(new { error = "이미 존재하는 Slug입니다." });

        var tenant = new Tenant
        {
            Slug = request.Slug,
            Name = request.Name,
            CustomDomain = request.CustomDomain,
            Subdomain = request.Subdomain ?? request.Slug,
            IsActive = true,
            ConfigJson = request.ConfigJson,
            CreatedBy = "PlatformAdmin"
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        // TenantAdmin 자동 발행: 관리자 이메일이 제공된 경우
        int? tenantAdminId = null;
        if (!string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            var tempPassword = Guid.NewGuid().ToString("N")[..12];
            var adminUser = new User
            {
                TenantId = tenant.Id,
                Username = request.AdminEmail.Split('@')[0],
                Email = request.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Name = request.AdminName ?? request.Name + " 관리자",
                Role = nameof(UserRole.TenantAdmin),
                IsActive = true,
                CreatedBy = "PlatformAdmin"
            };

            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync();
            tenantAdminId = adminUser.Id;
        }

        return Created($"/api/platform/tenants/{tenant.Slug}",
            new
            {
                tenant = new TenantDto(tenant.Id, tenant.Slug, tenant.Name, tenant.CustomDomain, tenant.Subdomain, tenant.IsActive, tenant.ConfigJson),
                tenantAdminId
            });
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateTenant(string slug, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        tenant.Name = request.Name ?? tenant.Name;
        tenant.CustomDomain = request.CustomDomain ?? tenant.CustomDomain;
        tenant.Subdomain = request.Subdomain ?? tenant.Subdomain;
        tenant.IsActive = request.IsActive ?? tenant.IsActive;
        tenant.ConfigJson = request.ConfigJson ?? tenant.ConfigJson;
        tenant.UpdatedBy = "PlatformAdmin";
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new TenantDto(tenant.Id, tenant.Slug, tenant.Name, tenant.CustomDomain, tenant.Subdomain, tenant.IsActive, tenant.ConfigJson));
    }
    [HttpGet("billing")]
    public async Task<IActionResult> GetAllBilling()
    {
        var result = await _mediator.Send(new GetTenantBillingQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{slug}/billing")]
    public async Task<IActionResult> GetTenantBilling(string slug)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new GetTenantBillingQuery(tenant.Id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data?.FirstOrDefault());
    }

    [HttpPut("{slug}/billing")]
    public async Task<IActionResult> UpdateTenantBilling(string slug, [FromBody] UpdateBillingRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        if (request.PlanType is not null)
        {
            var planResult = await _mediator.Send(new UpdateTenantPlanCommand(tenant.Id, request.PlanType, request.MonthlyPrice ?? 0));
            if (!planResult.IsSuccess)
                return BadRequest(new { error = planResult.Error });
        }

        if (request.BillingStatus is not null)
        {
            var statusResult = await _mediator.Send(new UpdateBillingStatusCommand(tenant.Id, request.BillingStatus));
            if (!statusResult.IsSuccess)
                return BadRequest(new { error = statusResult.Error });
        }

        return Ok(new { success = true });
    }
    // ── Invoices ──

    [HttpGet("invoices")]
    public async Task<IActionResult> GetAllInvoices()
    {
        var result = await _mediator.Send(new GetInvoicesQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{slug}/invoices")]
    public async Task<IActionResult> GetTenantInvoices(string slug)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(slug));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{slug}/invoices")]
    public async Task<IActionResult> GenerateInvoice(string slug, [FromBody] GenerateInvoiceRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new GenerateInvoiceCommand(tenant.Id, request.BillingPeriod));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { invoiceId = result.Data });
    }

    [HttpPut("invoices/{id:int}/pay")]
    public async Task<IActionResult> MarkInvoicePaid(int id, [FromBody] MarkInvoicePaidRequest request)
    {
        var result = await _mediator.Send(new MarkInvoicePaidCommand(id, request.TransactionId, request.PaymentMethod));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("{slug}/usage")]
    public async Task<IActionResult> GetTenantUsage(string slug)
    {
        var result = await _mediator.Send(new GetTenantUsageQuery(slug));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{slug}/domain")]
    public async Task<IActionResult> GetTenantDomainConfig(string slug)
    {
        var result = await _mediator.Send(new Application.Platform.Queries.GetTenantDomainConfigQuery(slug));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("{slug}/domain")]
    public async Task<IActionResult> UpdateTenantDomain(string slug, [FromBody] UpdateDomainRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new Application.Platform.Commands.UpdateTenantDomainCommand(
            tenant.Id, request.CustomDomain, request.Subdomain));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{slug}/domain/verify")]
    public async Task<IActionResult> VerifyTenantDomain(string slug)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new Application.Platform.Commands.VerifyTenantDomainCommand(tenant.Id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    // ── Commissions / Settlements ──

    [HttpGet("commissions/summary")]
    public async Task<IActionResult> GetCommissionSummary()
    {
        var result = await _mediator.Send(new GetCommissionSummaryQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{slug}/commissions/settings")]
    public async Task<IActionResult> GetCommissionSettings(string slug)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new GetCommissionSettingsQuery(tenant.Id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("{slug}/commissions/settings")]
    public async Task<IActionResult> UpsertCommissionSetting(string slug, [FromBody] UpsertCommissionSettingRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new UpsertCommissionSettingCommand(
            tenant.Id, request.ProductId, request.CategoryId,
            request.CommissionRate, request.SettlementCycle ?? "Weekly",
            request.SettlementDayOfWeek ?? 1, request.MinSettlementAmount ?? 10000m,
            request.BankName, request.BankAccount, request.BankHolder));

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { settingId = result.Data });
    }

    [HttpGet("{slug}/commissions")]
    public async Task<IActionResult> GetCommissions(string slug, [FromQuery] string? status = null)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new GetCommissionsQuery(tenant.Id, status));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("{slug}/settlements")]
    public async Task<IActionResult> GetSettlements(string slug, [FromQuery] string? status = null)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new GetSettlementsQuery(tenant.Id, status));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{slug}/settlements")]
    public async Task<IActionResult> CreateSettlement(string slug, [FromBody] CreateSettlementRequest request)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var result = await _mediator.Send(new CreateSettlementCommand(tenant.Id, request.PeriodStart, request.PeriodEnd));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { settlementId = result.Data });
    }

    [HttpPut("settlements/{id:int}/process")]
    public async Task<IActionResult> ProcessSettlement(int id, [FromBody] ProcessSettlementRequest request)
    {
        var result = await _mediator.Send(new ProcessSettlementCommand(id, request.TransactionId, request.SettledBy));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    // ── Tenant Seed Data ──

    [HttpPost("{slug}/seed")]
    public async Task<IActionResult> SeedTenantData(string slug, [FromBody] SeedTenantDataCommand command)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant is null)
            return NotFound(new { error = "테넌트를 찾을 수 없습니다." });

        var cmd = command with { TenantId = tenant.Id };
        var result = await _mediator.Send(cmd);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }
}

public record CreateTenantRequest(string Slug, string Name, string? CustomDomain, string? Subdomain, string? ConfigJson, string? AdminEmail, string? AdminName);
public record UpdateTenantRequest(string? Name, string? CustomDomain, string? Subdomain, bool? IsActive, string? ConfigJson);
public record UpdateBillingRequest(string? PlanType, decimal? MonthlyPrice, string? BillingStatus);
public record UpdateDomainRequest(string? CustomDomain, string? Subdomain);
public record GenerateInvoiceRequest(string BillingPeriod);
public record MarkInvoicePaidRequest(string? TransactionId, string? PaymentMethod);
public record UpsertCommissionSettingRequest(int? ProductId, int? CategoryId, decimal CommissionRate, string? SettlementCycle, int? SettlementDayOfWeek, decimal? MinSettlementAmount, string? BankName, string? BankAccount, string? BankHolder);
public record CreateSettlementRequest(DateTime PeriodStart, DateTime PeriodEnd);
public record ProcessSettlementRequest(string? TransactionId, string? SettledBy);
