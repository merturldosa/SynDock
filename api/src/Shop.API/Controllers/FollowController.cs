using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Follows.Commands;
using Shop.Application.Follows.Queries;

namespace Shop.API.Controllers;

[ApiController]
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
    public async Task<IActionResult> Toggle([FromBody] ToggleFollowRequest request)
    {
        var result = await _mediator.Send(new ToggleFollowCommand(request.TargetUserId));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { isFollowing = result.Data });
    }

    [HttpGet("followers/{userId:int}")]
    public async Task<IActionResult> GetFollowers(int userId)
    {
        var result = await _mediator.Send(new GetFollowersQuery(userId));
        return Ok(result);
    }

    [HttpGet("following/{userId:int}")]
    public async Task<IActionResult> GetFollowing(int userId)
    {
        var result = await _mediator.Send(new GetFollowingQuery(userId));
        return Ok(result);
    }

    [HttpGet("profile/{userId:int}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(userId));
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }
}

public record ToggleFollowRequest(int TargetUserId);
