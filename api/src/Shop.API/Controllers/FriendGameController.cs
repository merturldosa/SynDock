using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/social")]
[Authorize]
public class FriendGameController : ControllerBase
{
    private readonly IFriendGameService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FriendGameController> _logger;

    public FriendGameController(IFriendGameService service, ICurrentUserService currentUser,
        ITenantContext tenantContext, ILogger<FriendGameController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ───────────────────── Friends ─────────────────────

    /// <summary>Send friend request</summary>
    [HttpPost("friends/request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestDto dto, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var result = await _service.SendFriendRequestAsync(tenantId, userId, dto.AddresseeId, _currentUser.Username!, ct);
        return Ok(new { message = "친구 요청을 보냈습니다.", friendshipId = result.Id });
    }

    /// <summary>Accept friend request</summary>
    [HttpPost("friends/{id}/accept")]
    public async Task<IActionResult> AcceptFriendRequest(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        await _service.AcceptFriendRequestAsync(tenantId, id, userId, ct);
        return Ok(new { message = "친구 요청을 수락했습니다." });
    }

    /// <summary>Remove friend</summary>
    [HttpDelete("friends/{id}")]
    public async Task<IActionResult> RemoveFriend(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        await _service.RemoveFriendAsync(tenantId, id, userId, ct);
        return Ok(new { message = "친구를 삭제했습니다." });
    }

    /// <summary>My friends list</summary>
    [HttpGet("friends")]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var friends = await _service.GetFriendsAsync(tenantId, userId, ct);
        return Ok(friends);
    }

    /// <summary>Pending friend requests</summary>
    [HttpGet("friends/pending")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var pending = await _service.GetPendingRequestsAsync(tenantId, userId, ct);
        return Ok(pending.Select(f => new
        {
            f.Id,
            f.RequesterId,
            RequesterName = f.Requester?.Username,
            f.CreatedAt
        }));
    }

    // ───────────────────── Game Rooms ─────────────────────

    /// <summary>Create game room</summary>
    [HttpPost("games")]
    public async Task<IActionResult> CreateGameRoom([FromBody] CreateGameRoomDto dto, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var room = await _service.CreateGameRoomAsync(tenantId, userId, dto.GameType, dto.Mode,
            dto.TotalRounds, dto.BetType, dto.BetAmount, _currentUser.Username!, ct);
        return Ok(new { roomId = room.Id, roomCode = room.RoomCode, message = "게임 방이 생성되었습니다." });
    }

    /// <summary>Get game room details</summary>
    [HttpGet("games/{id}")]
    public async Task<IActionResult> GetGameRoom(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var room = await _service.GetGameRoomAsync(tenantId, id, ct);
        if (room == null) return NotFound();
        return Ok(MapGameRoom(room));
    }

    /// <summary>Join game room by code</summary>
    [HttpGet("games/join/{code}")]
    public async Task<IActionResult> GetGameRoomByCode(string code, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var room = await _service.GetGameRoomByCodeAsync(tenantId, code, ct);
        if (room == null) return NotFound(new { message = "방을 찾을 수 없습니다." });
        return Ok(MapGameRoom(room));
    }

    /// <summary>My active games</summary>
    [HttpGet("games/active")]
    public async Task<IActionResult> GetActiveGames(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var games = await _service.GetActiveGamesAsync(tenantId, userId, ct);
        return Ok(games.Select(MapGameRoom));
    }

    /// <summary>Join game room</summary>
    [HttpPost("games/{id}/join")]
    public async Task<IActionResult> JoinGame(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        await _service.JoinGameAsync(tenantId, id, userId, _currentUser.Username!, ct);
        return Ok(new { message = "게임에 참가했습니다." });
    }

    /// <summary>Start game (host only)</summary>
    [HttpPost("games/{id}/start")]
    public async Task<IActionResult> StartGame(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        await _service.StartGameAsync(tenantId, id, userId, ct);
        return Ok(new { message = "게임이 시작되었습니다." });
    }

    /// <summary>Get current game question</summary>
    [HttpGet("games/{id}/question")]
    public async Task<IActionResult> GetGameQuestion(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var result = await _service.GetGameQuestionAsync(tenantId, id, ct);
        return Ok(result);
    }

    /// <summary>Submit answer</summary>
    [HttpPost("games/{id}/answer")]
    public async Task<IActionResult> SubmitAnswer(int id, [FromBody] SubmitAnswerDto dto, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var result = await _service.SubmitAnswerAsync(tenantId, id, userId, dto.Answer, ct);
        return Ok(result);
    }

    /// <summary>End game</summary>
    [HttpPost("games/{id}/end")]
    public async Task<IActionResult> EndGame(int id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var result = await _service.EndGameAsync(tenantId, id, ct);
        return Ok(result);
    }

    /// <summary>My game stats</summary>
    [HttpGet("games/stats")]
    public async Task<IActionResult> GetGameStats(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _currentUser.UserId!.Value;
        var stats = await _service.GetGameStatsAsync(tenantId, userId, ct);
        return Ok(stats);
    }

    // ───────────────────── Helpers ─────────────────────

    private static object MapGameRoom(GameRoom room) => new
    {
        room.Id,
        room.RoomCode,
        room.GameType,
        room.Mode,
        room.Status,
        room.HostUserId,
        HostName = room.Host?.Username,
        room.MaxPlayers,
        room.CurrentRound,
        room.TotalRounds,
        room.BetType,
        room.BetAmount,
        room.TotalPot,
        room.StartedAt,
        room.FinishedAt,
        PlayerCount = room.Players?.Count ?? 0,
        Players = room.Players?.Select(p => new
        {
            p.UserId,
            Username = p.User?.Username,
            p.Score,
            p.CorrectAnswers,
            p.Rank,
            p.IsReady,
            p.HasBet,
            p.WinAmount
        })
    };
}

// ───────────────────── DTOs ─────────────────────

public record FriendRequestDto(int AddresseeId);

public record CreateGameRoomDto(
    string GameType,
    string Mode = "1v1",
    int TotalRounds = 5,
    string BetType = "None",
    decimal BetAmount = 0
);

public record SubmitAnswerDto(string Answer);
