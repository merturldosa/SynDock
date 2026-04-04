using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Follows.Commands;
using Shop.Application.Follows.Queries;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class FollowController : ControllerBase
{
    private readonly IMediator _mediator;

    public FollowController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("toggle")]
    [Authorize]
    public async Task<IActionResult> Toggle([FromBody] ToggleFollowRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleFollowCommand(request.TargetUserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { isFollowing = result.Data });
    }

    [HttpGet("followers/{userId:int}")]
    public async Task<IActionResult> GetFollowers(int userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFollowersQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("following/{userId:int}")]
    public async Task<IActionResult> GetFollowing(int userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFollowingQuery(userId), ct);
        return Ok(result);
    }

    [HttpGet("profile/{userId:int}")]
    public async Task<IActionResult> GetProfile(int userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(userId), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }
}

public record ToggleFollowRequest(int TargetUserId);
