using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.QnAs.Commands;
using Shop.Application.QnAs.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QnAController : ControllerBase
{
    private readonly IMediator _mediator;

    public QnAController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductQnAsQuery(productId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyQnAs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyQnAsQuery(page, pageSize), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQnARequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateQnACommand(request.ProductId, request.Title, request.Content, request.IsSecret), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { qnaId = result.Data });
    }

    [HttpPost("{id:int}/answer")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> Answer(int id, [FromBody] AnswerQnARequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AnswerQnACommand(id, request.Content), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { replyId = result.Data });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteQnACommand(id), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}

public record CreateQnARequest(int ProductId, string Title, string Content, bool IsSecret = false);
public record AnswerQnARequest(string Content);
