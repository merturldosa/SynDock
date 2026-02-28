using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
