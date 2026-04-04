using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrmController : ControllerBase
{
    private readonly ICrmService _crm;
    private readonly ICurrentUserService _currentUser;

    public CrmController(ICrmService crm, ICurrentUserService currentUser)
    {
        _crm = crm;
        _currentUser = currentUser;
    }

    // === Segments ===
    [HttpGet("segments")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetSegments(CancellationToken ct)
        => Ok(await _crm.GetSegmentsAsync(0, ct));

    [HttpPost("segments")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CreateSegment([FromBody] CreateSegmentRequest req, CancellationToken ct)
        => Ok(await _crm.CreateSegmentAsync(0, req.Name, req.Type, req.Description, req.RulesJson, _currentUser.Username ?? "system", ct));

    [HttpPost("segments/{id}/recalculate")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> RecalculateSegment(int id, CancellationToken ct)
    {
        await _crm.RecalculateSegmentAsync(0, id, ct);
        return Ok(new { message = "Segment recalculated" });
    }

    // === Tags ===
    [HttpGet("tags")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetTags(CancellationToken ct)
        => Ok(await _crm.GetTagsAsync(0, ct));

    [HttpPost("tags")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest req, CancellationToken ct)
        => Ok(await _crm.CreateTagAsync(0, req.Name, req.Color, req.Description, _currentUser.Username ?? "system", ct));

    [HttpPost("tags/{tagId}/assign/{userId}")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> AssignTag(int tagId, int userId, CancellationToken ct)
    {
        await _crm.AssignTagAsync(0, userId, tagId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Tag assigned" });
    }

    [HttpDelete("tags/{tagId}/assign/{userId}")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> RemoveTag(int tagId, int userId, CancellationToken ct)
    {
        await _crm.RemoveTagAsync(0, userId, tagId, ct);
        return Ok(new { message = "Tag removed" });
    }

    [HttpGet("users/{userId}/tags")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetUserTags(int userId, CancellationToken ct)
        => Ok(await _crm.GetUserTagsAsync(0, userId, ct));

    // === CS Tickets ===
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? 0;
        return Ok(await _crm.CreateTicketAsync(0, userId, req.Subject, req.Category, req.Priority, req.Content, req.OrderId, _currentUser.Username ?? "system", ct));
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] string? status, [FromQuery] string? category, [FromQuery] int? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        // Non-admin users can only see their own tickets
        if (!User.IsInRole("Admin") && !User.IsInRole("TenantAdmin") && !User.IsInRole("PlatformAdmin"))
            userId = _currentUser.UserId ?? 0;

        var (items, totalCount) = await _crm.GetTicketsAsync(0, status, category, userId, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) });
    }

    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(int id, CancellationToken ct)
    {
        var ticket = await _crm.GetTicketAsync(0, id, ct);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets/{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] ReplyTicketRequest req, CancellationToken ct)
    {
        var senderId = _currentUser.UserId ?? 0;
        var senderType = User.IsInRole("Admin") || User.IsInRole("TenantAdmin") ? "Agent" : "Customer";
        return Ok(await _crm.ReplyToTicketAsync(0, id, senderId, senderType, req.Content, req.AttachmentUrl, req.IsInternal, _currentUser.Username ?? "system", ct));
    }

    [HttpPut("tickets/{id}/status")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] UpdateTicketStatusRequest req, CancellationToken ct)
    {
        await _crm.UpdateTicketStatusAsync(0, id, req.Status, req.AssignedToUserId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Ticket status updated" });
    }

    [HttpPost("tickets/{id}/rate")]
    public async Task<IActionResult> RateTicket(int id, [FromBody] RateTicketRequest req, CancellationToken ct)
    {
        await _crm.RateTicketAsync(0, id, req.Rating, req.Comment, ct);
        return Ok(new { message = "Rating submitted" });
    }

    // === Journey ===
    [HttpPost("journey/track")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? 0;
        await _crm.TrackEventAsync(0, userId, req.EventType, req.EventDetail, req.ReferenceId, req.ReferenceType, req.MetadataJson, req.Channel, req.SessionId, ct);
        return Ok(new { message = "Event tracked" });
    }

    [HttpGet("journey/{userId}")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetCustomerJourney(int userId, [FromQuery] int limit = 100, CancellationToken ct = default)
        => Ok(await _crm.GetCustomerJourneyAsync(0, userId, limit, ct));

    // === Analytics ===
    [HttpGet("analytics/customers")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetCustomerAnalytics(CancellationToken ct)
        => Ok(await _crm.GetCustomerAnalyticsAsync(0, ct));

    [HttpGet("analytics/tickets")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetTicketAnalytics([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
        => Ok(await _crm.GetTicketAnalyticsAsync(0, from, to, ct));

    // === Customer 360 ===
    [HttpGet("customers/{userId}/360")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetCustomer360(int userId, CancellationToken ct)
        => Ok(await _crm.GetCustomer360Async(0, userId, ct));

    // === Lead Scoring ===
    [HttpPost("leads/{userId}/calculate")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CalculateLeadScore(int userId, CancellationToken ct)
        => Ok(await _crm.CalculateLeadScoreAsync(0, userId, ct));

    [HttpPost("leads/recalculate-all")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> RecalculateAllLeadScores(CancellationToken ct)
    {
        await _crm.RecalculateAllLeadScoresAsync(0, ct);
        return Ok(new { message = "All lead scores recalculated" });
    }

    [HttpGet("leads/top")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetTopLeads([FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(await _crm.GetTopLeadsAsync(0, limit, ct));

    // === VoC ===
    [HttpPost("voc")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CreateVocEntry([FromBody] CreateVocRequest req, CancellationToken ct)
    {
        var userId = req.UserId ?? _currentUser.UserId;
        return Ok(await _crm.CreateVocEntryAsync(0, userId, req.Source, req.SourceId, req.Content, _currentUser.Username ?? "system", ct));
    }

    [HttpGet("voc")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetVocEntries([FromQuery] string? sentiment, [FromQuery] string? source, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _crm.GetVocEntriesAsync(0, sentiment, source, page, pageSize, ct));

    [HttpGet("voc/dashboard")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetVocDashboard(CancellationToken ct)
        => Ok(await _crm.GetVocDashboardAsync(0, ct));

    // === Sales Pipeline ===
    [HttpPost("pipelines")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> CreatePipeline([FromBody] CreatePipelineRequest req, CancellationToken ct)
        => Ok(await _crm.CreatePipelineAsync(0, req.UserId, req.Title, req.ExpectedValue, req.ExpectedCloseDate, req.AssignedTo, _currentUser.Username ?? "system", ct));

    [HttpGet("pipelines")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetPipelines([FromQuery] string? stage, CancellationToken ct = default)
        => Ok(await _crm.GetPipelinesAsync(0, stage, ct));

    [HttpPut("pipelines/{id}/stage")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> UpdatePipelineStage(int id, [FromBody] UpdatePipelineStageRequest req, CancellationToken ct)
    {
        await _crm.UpdatePipelineStageAsync(0, id, req.Stage, req.Notes, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Pipeline stage updated" });
    }

    [HttpGet("pipelines/dashboard")]
    [Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
    public async Task<IActionResult> GetPipelineDashboard(CancellationToken ct)
        => Ok(await _crm.GetPipelineDashboardAsync(0, ct));
}

// Request DTOs
public record CreateSegmentRequest(string Name, string Type = "Manual", string? Description = null, string? RulesJson = null);
public record CreateTagRequest(string Name, string Color = "#3B82F6", string? Description = null);
public record CreateTicketRequest(string Subject, string Category = "General", string Priority = "Normal", string Content = "", int? OrderId = null);
public record ReplyTicketRequest(string Content, string? AttachmentUrl = null, bool IsInternal = false);
public record UpdateTicketStatusRequest(string Status, int? AssignedToUserId = null);
public record RateTicketRequest(int Rating, string? Comment = null);
public record TrackEventRequest(string EventType, string? EventDetail = null, int? ReferenceId = null, string? ReferenceType = null, string? MetadataJson = null, string? Channel = null, string? SessionId = null);
public record CreateVocRequest(string Source, string Content, int? UserId = null, int? SourceId = null);
public record CreatePipelineRequest(string Title, decimal ExpectedValue, int? UserId = null, DateTime? ExpectedCloseDate = null, string? AssignedTo = null);
public record UpdatePipelineStageRequest(string Stage, string? Notes = null);
