using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Chat.Commands;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        var messages = request.Messages
            .Select(m => new ChatMessageRequest(m.Role, m.Content))
            .ToList();

        var result = await _mediator.Send(new SendChatCommand(messages), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { content = result.Data });
    }
}

public record ChatMessageDto(string Role, string Content);
public record ChatRequest(List<ChatMessageDto> Messages);
