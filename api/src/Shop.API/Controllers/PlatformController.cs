using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/platform/tenants")]
public class PlatformController : ControllerBase
{
    private readonly ShopDbContext _db;

    public PlatformController(ShopDbContext db)
    {
        _db = db;
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

        return Created($"/api/platform/tenants/{tenant.Slug}",
            new TenantDto(tenant.Id, tenant.Slug, tenant.Name, tenant.CustomDomain, tenant.Subdomain, tenant.IsActive, tenant.ConfigJson));
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
}

public record CreateTenantRequest(string Slug, string Name, string? CustomDomain, string? Subdomain, string? ConfigJson);
public record UpdateTenantRequest(string? Name, string? CustomDomain, string? Subdomain, bool? IsActive, string? ConfigJson);
