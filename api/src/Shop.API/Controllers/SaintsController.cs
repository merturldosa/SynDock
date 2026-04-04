using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Saints.Commands;
using Shop.Application.Saints.Queries;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
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
    public async Task<IActionResult> GetSaints([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSaintsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSaint(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSaintByIdQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodaySaints(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var result = await _mediator.Send(new GetSaintsByFeastDayQuery(now.Month, now.Day), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/products")]
    public async Task<IActionResult> GetProductsBySaint(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsBySaintQuery(id), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("seed")]
    public async Task<IActionResult> SeedSaints(CancellationToken ct)
    {
        var result = await _mediator.Send(new SeedSaintsCommand(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { count = result.Data });
    }
}
