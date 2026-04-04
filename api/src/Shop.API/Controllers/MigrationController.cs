using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "PlatformAdmin")]
public class MigrationController : ControllerBase
{
    private readonly IShopMigrationService _migration;
    private readonly ICurrentUserService _currentUser;

    public MigrationController(IShopMigrationService migration, ICurrentUserService currentUser)
    {
        _migration = migration;
        _currentUser = currentUser;
    }

    /// <summary>Preview: crawl a URL without importing (PlatformAdmin only)</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest req, CancellationToken ct)
    {
        var result = await _migration.PreviewMigrationAsync(req.SourceUrl, ct);
        return Ok(result);
    }

    /// <summary>Start migration for a tenant</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartMigration([FromBody] StartMigrationRequest req, CancellationToken ct)
    {
        var job = await _migration.StartMigrationAsync(req.TenantId, req.ApplicationId, req.SourceUrl, req.SourceType ?? "Website", _currentUser.Username ?? "system", ct);
        return Ok(job);
    }

    /// <summary>Start migration during provisioning (PlatformAdmin only)</summary>
    [HttpPost("start-for-application")]
    public async Task<IActionResult> StartForApplication([FromBody] ApplicationMigrationRequest req, CancellationToken ct)
    {
        var job = await _migration.StartMigrationAsync(null, req.ApplicationId, req.SourceUrl, req.SourceType ?? "Website", req.Email, ct);
        return Ok(new { jobId = job.Id, message = "Migration started" });
    }

    /// <summary>Get migration job status</summary>
    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(int jobId, CancellationToken ct)
    {
        var job = await _migration.GetJobAsync(jobId, ct);
        return job == null ? NotFound() : Ok(job);
    }

    /// <summary>List migration jobs for tenant</summary>
    [HttpGet]
    public async Task<IActionResult> GetJobs([FromQuery] int? tenantId, CancellationToken ct)
        => Ok(await _migration.GetJobsAsync(tenantId, ct));

    /// <summary>Import crawl results to a tenant (after review)</summary>
    [HttpPost("{jobId}/import")]
    public async Task<IActionResult> ImportResults(int jobId, [FromBody] ImportRequest req, CancellationToken ct)
    {
        await _migration.ImportCrawlResultsAsync(jobId, req.TenantId, ct);
        return Ok(new { message = "Import completed" });
    }
}

public record PreviewRequest(string SourceUrl);
public record StartMigrationRequest(int? TenantId, int? ApplicationId, string SourceUrl, string? SourceType = null);
public record ApplicationMigrationRequest(int ApplicationId, string SourceUrl, string Email, string? SourceType = null);
public record ImportRequest(int TenantId);
