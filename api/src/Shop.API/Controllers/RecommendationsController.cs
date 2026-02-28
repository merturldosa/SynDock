using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Recommendations.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecommendationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetProductRecommendations(int productId, [FromQuery] int count = 6)
    {
        var result = await _mediator.Send(new GetProductRecommendationsQuery(productId, count));
        return Ok(result.Data);
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUserRecommendations([FromQuery] int count = 6)
    {
        var result = await _mediator.Send(new GetUserRecommendationsQuery(count));
        return Ok(result.Data);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularProducts([FromQuery] int count = 6)
    {
        var result = await _mediator.Send(new GetPopularProductsQuery(count));
        return Ok(result.Data);
    }
}
