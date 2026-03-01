using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Admin.Commands;
using Shop.Application.Admin.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
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
}

public record UpdateStockRequest(int VariantId, int NewStock);
public record BulkUpdateOrderStatusRequest(int[] OrderIds, string Status);
