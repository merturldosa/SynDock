using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Saints.Commands;
using Shop.Application.Saints.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SaintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SaintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSaints([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSaintsQuery(search, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSaint(int id)
    {
        var result = await _mediator.Send(new GetSaintByIdQuery(id));
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodaySaints()
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(new GetSaintsByFeastDayQuery(now.Month, now.Day));
        return Ok(result);
    }

    [Authorize]
    [HttpPost("seed")]
    public async Task<IActionResult> SeedSaints()
    {
        var result = await _mediator.Send(new SeedSaintsCommand());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { count = result.Data });
    }
}
