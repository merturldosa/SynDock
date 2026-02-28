using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Hashtags.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HashtagController : ControllerBase
{
    private readonly IMediator _mediator;

    public HashtagController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetTrendingHashtagsQuery(limit));
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Ok(Array.Empty<object>());
        var result = await _mediator.Send(new SearchHashtagsQuery(keyword));
        return Ok(result);
    }

    [HttpGet("{tag}/posts")]
    public async Task<IActionResult> GetPostsByHashtag(string tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPostsByHashtagQuery(tag, page, pageSize));
        return Ok(result);
    }
}
