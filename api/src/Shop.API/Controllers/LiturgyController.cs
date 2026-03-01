using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Liturgy.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LiturgyController : ControllerBase
{
    private readonly IMediator _mediator;

    public LiturgyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayLiturgy()
    {
        var result = await _mediator.Send(new GetCurrentLiturgyQuery());
        return Ok(result);
    }

    [HttpGet("seasons")]
    public async Task<IActionResult> GetSeasons([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var result = await _mediator.Send(new GetLiturgicalSeasonsQuery(targetYear));
        return Ok(result);
    }
}
