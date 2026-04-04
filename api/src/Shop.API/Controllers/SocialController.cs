using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/social")]
[Authorize]
public class SocialController : ControllerBase
{
    private readonly ISocialCommerceService _social;
    private readonly ICurrentUserService _currentUser;

    public SocialController(ISocialCommerceService social, ICurrentUserService currentUser)
    {
        _social = social;
        _currentUser = currentUser;
    }

    private int UserId => _currentUser.UserId ?? 0;
    private string Username => _currentUser.Username ?? "system";

    // ===== Member Grades =====

    /// <summary>My grade info</summary>
    [HttpGet("grade")]
    public async Task<IActionResult> GetMyGrade(CancellationToken ct)
        => Ok(await _social.GetOrCreateGradeAsync(0, UserId, ct));

    /// <summary>Grade benefits table</summary>
    [HttpGet("grade/benefits")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGradeBenefits(CancellationToken ct)
        => Ok(await _social.GetGradeBenefitsAsync("", ct));

    /// <summary>Top members leaderboard</summary>
    [HttpGet("grade/top")]
    public async Task<IActionResult> GetTopMembers([FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await _social.GetTopMembersAsync(0, limit, ct));

    // ===== Gifts =====

    /// <summary>Send a gift</summary>
    [HttpPost("gifts")]
    public async Task<IActionResult> SendGift([FromBody] SendGiftRequest req, CancellationToken ct)
    {
        var gift = await _social.SendGiftAsync(0, UserId, req.ToUserId, req.GiftType, req.Amount,
            req.Message, req.TriggerType, req.TriggerReferenceId, Username, ct);
        return Ok(gift);
    }

    /// <summary>Gifts I received</summary>
    [HttpGet("gifts/received")]
    public async Task<IActionResult> GetGiftsReceived([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _social.GetGiftsReceivedAsync(0, UserId, page, pageSize, ct));

    /// <summary>Gifts I sent</summary>
    [HttpGet("gifts/sent")]
    public async Task<IActionResult> GetGiftsSent([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _social.GetGiftsSentAsync(0, UserId, page, pageSize, ct));

    /// <summary>Thank a gift</summary>
    [HttpPost("gifts/{id}/thank")]
    public async Task<IActionResult> ThankGift(int id, CancellationToken ct)
    {
        await _social.ThankGiftAsync(0, id, UserId, ct);
        return Ok(new { message = "Gift thanked" });
    }

    // ===== Chat =====

    /// <summary>Get or create chat room</summary>
    [HttpPost("chat/room")]
    public async Task<IActionResult> GetOrCreateChatRoom([FromBody] CreateChatRoomRequest req, CancellationToken ct)
    {
        var room = await _social.GetOrCreateChatRoomAsync(0, UserId, req.OtherUserId, req.ProductId, req.ReviewId, ct);
        return Ok(room);
    }

    /// <summary>My chat rooms</summary>
    [HttpGet("chat/rooms")]
    public async Task<IActionResult> GetMyChatRooms(CancellationToken ct)
        => Ok(await _social.GetMyChatRoomsAsync(0, UserId, ct));

    /// <summary>Send message</summary>
    [HttpPost("chat/{roomId}/message")]
    public async Task<IActionResult> SendMessage(int roomId, [FromBody] SendChatMessageRequest req, CancellationToken ct)
    {
        var msg = await _social.SendMessageAsync(0, roomId, UserId, req.Content, req.MessageType ?? "Text", req.GiftId, Username, ct);
        return Ok(msg);
    }

    /// <summary>Get messages</summary>
    [HttpGet("chat/{roomId}/messages")]
    public async Task<IActionResult> GetMessages(int roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _social.GetMessagesAsync(0, roomId, UserId, page, pageSize, ct));

    /// <summary>Mark as read</summary>
    [HttpPut("chat/{roomId}/read")]
    public async Task<IActionResult> MarkAsRead(int roomId, CancellationToken ct)
    {
        await _social.MarkAsReadAsync(0, roomId, UserId, ct);
        return Ok(new { message = "Messages marked as read" });
    }
}

// === Request DTOs ===

public record SendGiftRequest(
    int ToUserId,
    string GiftType,
    decimal Amount,
    string? Message = null,
    string? TriggerType = null,
    int? TriggerReferenceId = null
);

public record CreateChatRoomRequest(
    int OtherUserId,
    int? ProductId = null,
    int? ReviewId = null
);

public record SendChatMessageRequest(
    string Content,
    string? MessageType = "Text",
    int? GiftId = null
);
