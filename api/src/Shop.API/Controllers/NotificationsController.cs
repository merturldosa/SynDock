using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Notifications.Commands;
using Shop.Application.Notifications.Queries;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(page, pageSize));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _mediator.Send(new MarkAllAsReadCommand());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { markedCount = result.Data });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand(id));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }
}
