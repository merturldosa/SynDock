using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Liturgy.Queries;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class LiturgyController : ControllerBase
{
    private readonly IMediator _mediator;

    public LiturgyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayLiturgy(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentLiturgyQuery(), ct);
        return Ok(result);
    }

    [HttpGet("seasons")]
    public async Task<IActionResult> GetSeasons([FromQuery] int? year, CancellationToken ct)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var result = await _mediator.Send(new GetLiturgicalSeasonsQuery(targetYear), ct);
        return Ok(result);
    }
}
