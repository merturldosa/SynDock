using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Admin.Commands;
using Shop.Application.Admin.Queries;
using Shop.Application.Common.Interfaces;
using Shop.Application.Notifications.Commands;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly IShopDbContext _db;

    public AdminController(IMediator mediator, IEmailService emailService, IShopDbContext db)
    {
        _mediator = mediator;
        _emailService = emailService;
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _mediator.Send(new GetUsersQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, request.Role, request.IsActive));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetSalesAnalytics(
        [FromQuery] int days = 30,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeComparison = false)
    {
        var result = await _mediator.Send(new GetSalesAnalyticsQuery(days, startDate, endDate, includeComparison));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("analytics/customers")]
    public async Task<IActionResult> GetCustomerAnalytics()
    {
        var result = await _mediator.Send(new GetCustomerAnalyticsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("analytics/products")]
    public async Task<IActionResult> GetProductPerformance(
        [FromQuery] string? sort = "revenue",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetProductPerformanceQuery(sort, page, pageSize));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAdminOrders(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetAdminOrdersQuery(status, search, page, pageSize));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("export/sales")]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await _mediator.Send(new GetSalesReportQuery(startDate, endDate));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(result.Data!)).ToArray();
        return File(bytes, "text/csv", $"sales-report-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.csv");
    }

    [HttpGet("export/orders")]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetOrderExportQuery(status, startDate, endDate, search));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(result.Data!)).ToArray();
        return File(bytes, "text/csv", $"orders-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 10)
    {
        var result = await _mediator.Send(new GetLowStockQuery(threshold));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("stock")]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateStockRequest request)
    {
        var result = await _mediator.Send(new UpdateStockCommand(request.VariantId, request.NewStock));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("orders/bulk-status")]
    public async Task<IActionResult> BulkUpdateOrderStatus([FromBody] BulkUpdateOrderStatusRequest request)
    {
        var result = await _mediator.Send(new BulkUpdateOrderStatusCommand(request.OrderIds, request.Status));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        var result = await _mediator.Send(new BroadcastNotificationCommand(request.Title, request.Message, request.Type));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { sentCount = result.Data });
    }
    [HttpPost("email/broadcast")]
    public async Task<IActionResult> SendMarketingEmail([FromBody] MarketingEmailRequest request)
    {
        var usersQuery = _db.Users.AsNoTracking()
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email));

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        usersQuery = request.Target switch
        {
            "new_users" => usersQuery.Where(u => u.CreatedAt >= thirtyDaysAgo),
            "vip" => usersQuery.Where(u => u.Role == "VIP" || u.Role == "Admin"),
            "inactive" => usersQuery.Where(u => u.CreatedAt < thirtyDaysAgo),
            _ => usersQuery
        };

        var emails = await usersQuery.Select(u => u.Email).ToListAsync();

        var htmlBody = Shop.Infrastructure.Services.EmailTemplates.MarketingBroadcast(request.Title, request.Content);
        var sentCount = 0;

        foreach (var email in emails)
        {
            try
            {
                await _emailService.SendAsync(email, request.Title, htmlBody);
                sentCount++;
            }
            catch { /* skip failed emails */ }
        }

        return Ok(new { sentCount });
    }

    // ── Email Campaigns ──

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns()
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetCampaignsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.CreateCampaignCommand(
            request.Title, request.Content, request.Target, request.ScheduledAt));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { campaignId = result.Data });
    }

    [HttpPost("campaigns/{id:int}/send")]
    public async Task<IActionResult> SendCampaign(int id)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.SendCampaignCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { sentCount = result.Data });
    }

    [HttpPost("campaigns/ab-test")]
    public async Task<IActionResult> CreateAbTestCampaign([FromBody] CreateAbTestCampaignRequest request)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.CreateAbTestCampaignCommand(
            request.Title, request.Target, request.ScheduledAt,
            request.SubjectLineA, request.ContentA,
            request.SubjectLineB, request.ContentB,
            request.TrafficPercentA));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { campaignId = result.Data });
    }

    [HttpGet("campaigns/{id:int}/analytics")]
    public async Task<IActionResult> GetCampaignAnalytics(int id)
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetCampaignAnalyticsQuery(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("campaigns/summary")]
    public async Task<IActionResult> GetCampaignSummary()
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetCampaignSummaryQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("campaigns/{id:int}/track")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackCampaignEvent(int id, [FromBody] TrackCampaignEventRequest request)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.RecordCampaignEventCommand(
            id, request.VariantId, request.UserId, request.EventType, request.LinkUrl));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
    // ── SNS Auto-Posting ──

    [HttpPost("social/post/{productId:int}")]
    public async Task<IActionResult> AutoPostToSocial(int productId)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.AutoPostProductCommand(productId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("social/posts")]
    public async Task<IActionResult> GetSocialPosts([FromQuery] int? productId = null, [FromQuery] string? platform = null)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.GetSocialPostsQuery(productId, platform));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("billing")]
    public async Task<IActionResult> GetMyBilling()
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetMyTenantBillingQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    // ── Settlements / Commissions (TenantAdmin) ──

    [HttpGet("settlements")]
    public async Task<IActionResult> GetMySettlements([FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetMySettlementsQuery(status));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("commissions")]
    public async Task<IActionResult> GetMyCommissions([FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetMyCommissionsQuery(status));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("commissions/settings")]
    public async Task<IActionResult> GetMyCommissionSettings()
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetMyCommissionSettingsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetTenantSettings()
    {
        var result = await _mediator.Send(new Application.Admin.Queries.GetTenantSettingsQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateTenantSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        var result = await _mediator.Send(new Application.Admin.Commands.UpdateTenantSettingsCommand(
            request.CompanyName, request.CompanyAddress, request.BusinessNumber,
            request.CeoName, request.ContactPhone, request.ContactEmail,
            request.HeroSubtitle, request.HeroTagline, request.HeroDescription,
            request.Theme != null ? new Application.Admin.Commands.TenantThemeDto(
                request.Theme.Primary, request.Theme.PrimaryLight,
                request.Theme.Secondary, request.Theme.SecondaryLight, request.Theme.Background) : null,
            request.LogoUrl, request.FaviconUrl));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record UpdateStockRequest(int VariantId, int NewStock);
public record BulkUpdateOrderStatusRequest(int[] OrderIds, string Status);
public record BroadcastNotificationRequest(string Title, string Message, string Type);
public record MarketingEmailRequest(string Title, string Content, string Target = "all");
public record CreateCampaignRequest(string Title, string Content, string Target = "all", DateTime? ScheduledAt = null);
public record CreateAbTestCampaignRequest(
    string Title, string Target, DateTime? ScheduledAt,
    string SubjectLineA, string ContentA,
    string SubjectLineB, string ContentB,
    int TrafficPercentA = 50);
public record TrackCampaignEventRequest(int? VariantId, int UserId, string EventType, string? LinkUrl = null);
public record UpdateTenantSettingsThemeRequest(string? Primary, string? PrimaryLight, string? Secondary, string? SecondaryLight, string? Background);
public record UpdateUserRequest(string Role, bool IsActive);
public record UpdateTenantSettingsRequest(
    string? CompanyName, string? CompanyAddress, string? BusinessNumber,
    string? CeoName, string? ContactPhone, string? ContactEmail,
    string? HeroSubtitle, string? HeroTagline, string? HeroDescription,
    UpdateTenantSettingsThemeRequest? Theme, string? LogoUrl, string? FaviconUrl);
