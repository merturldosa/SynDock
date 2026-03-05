using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Wishlists.Commands;
using Shop.Application.Wishlists.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWishlistQuery(), ct);
        return Ok(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] ToggleWishlistRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleWishlistCommand(request.ProductId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { isWished = result.Data });
    }

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] CheckWishlistRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckWishlistQuery(request.ProductIds), ct);
        return Ok(new { wishedProductIds = result });
    }

    [HttpPost("share")]
    public async Task<IActionResult> Share(CancellationToken ct)
    {
        var result = await _mediator.Send(new ShareWishlistCommand(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { shareToken = result.Data });
    }

    [HttpGet("shared/{token:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShared(Guid token, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSharedWishlistQuery(token), ct);
        return Ok(result);
    }
}

public record ToggleWishlistRequest(int ProductId);
public record CheckWishlistRequest(List<int> ProductIds);
