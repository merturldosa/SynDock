using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
