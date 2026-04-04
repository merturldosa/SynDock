using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Posts.Commands;
using Shop.Application.Posts.Queries;

namespace Shop.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? postType = null,
        [FromQuery] int? userId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeedQuery(page, pageSize, postType, userId), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPostByIdQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePostCommand(
            request.Title, request.Content, request.PostType,
            request.ProductId, request.ImageUrls, request.Hashtags), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { postId = result.Data });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePostCommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("{id:int}/comment")]
    [Authorize]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddCommentCommand(id, request.Content, request.ParentId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { commentId = result.Data });
    }

    [HttpPost("{id:int}/reaction")]
    [Authorize]
    public async Task<IActionResult> ToggleReaction(int id, [FromBody] ToggleReactionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleReactionCommand(id, request.ReactionType), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { isReacted = result.Data });
    }
}

public record CreatePostRequest(string? Title, string Content, string PostType, int? ProductId, List<string>? ImageUrls, List<string>? Hashtags);
public record AddCommentRequest(string Content, int? ParentId);
public record ToggleReactionRequest(string ReactionType);
