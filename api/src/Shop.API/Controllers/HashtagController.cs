using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Hashtags.Queries;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class HashtagController : ControllerBase
{
    private readonly IMediator _mediator;

    public HashtagController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTrendingHashtagsQuery(limit), ct);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Ok(Array.Empty<object>());
        var result = await _mediator.Send(new SearchHashtagsQuery(keyword), ct);
        return Ok(result);
    }

    [HttpGet("{tag}/posts")]
    public async Task<IActionResult> GetPostsByHashtag(string tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPostsByHashtagQuery(tag, page, pageSize), ct);
        return Ok(result);
    }
}
