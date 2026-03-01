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

    [HttpGet("analytics")]
    public async Task<IActionResult> GetSalesAnalytics([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetSalesAnalyticsQuery(days));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
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
}

public record UpdateStockRequest(int VariantId, int NewStock);
public record BulkUpdateOrderStatusRequest(int[] OrderIds, string Status);
public record BroadcastNotificationRequest(string Title, string Message, string Type);
public record MarketingEmailRequest(string Title, string Content, string Target = "all");
