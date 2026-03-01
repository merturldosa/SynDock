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
[Authorize]
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
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        var htmlBody = Shop.Infrastructure.Services.EmailTemplates.MarketingBroadcast(request.Title, request.Content);
        var sentCount = 0;

        foreach (var email in users)
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
public record UpdateTenantSettingsThemeRequest(string? Primary, string? PrimaryLight, string? Secondary, string? SecondaryLight, string? Background);
public record UpdateUserRequest(string Role, bool IsActive);
public record UpdateTenantSettingsRequest(
    string? CompanyName, string? CompanyAddress, string? BusinessNumber,
    string? CeoName, string? ContactPhone, string? ContactEmail,
    string? HeroSubtitle, string? HeroTagline, string? HeroDescription,
    UpdateTenantSettingsThemeRequest? Theme, string? LogoUrl, string? FaviconUrl);
