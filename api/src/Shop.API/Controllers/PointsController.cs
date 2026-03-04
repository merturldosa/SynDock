using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Points.Commands;
using Shop.Application.Points.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PointsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PointsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var result = await _mediator.Send(new GetPointBalanceQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPointHistoryQuery(page, pageSize));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    // ── Admin Endpoints ──

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("earn")]
    public async Task<IActionResult> EarnPoints([FromBody] EarnPointsRequest request)
    {
        var result = await _mediator.Send(new EarnPointsCommand(request.UserId, request.Amount, request.Description, request.OrderId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("use")]
    public async Task<IActionResult> UsePoints([FromBody] UsePointsRequest request)
    {
        var result = await _mediator.Send(new UsePointsCommand(request.UserId, request.Amount, request.OrderId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("refund")]
    public async Task<IActionResult> RefundPoints([FromBody] RefundPointsRequest request)
    {
        var result = await _mediator.Send(new RefundPointsCommand(request.UserId, request.Amount, request.OrderId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record EarnPointsRequest(int UserId, decimal Amount, string? Description, int? OrderId);
public record UsePointsRequest(int UserId, decimal Amount, int OrderId);
public record RefundPointsRequest(int UserId, decimal Amount, int OrderId);
