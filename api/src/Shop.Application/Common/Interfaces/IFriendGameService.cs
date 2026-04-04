using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IFriendGameService
{
    // Friends
    Task<Friendship> SendFriendRequestAsync(int tenantId, int requesterId, int addresseeId, string createdBy, CancellationToken ct = default);
    Task AcceptFriendRequestAsync(int tenantId, int friendshipId, int userId, CancellationToken ct = default);
    Task RemoveFriendAsync(int tenantId, int friendshipId, int userId, CancellationToken ct = default);
    Task<List<object>> GetFriendsAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<List<Friendship>> GetPendingRequestsAsync(int tenantId, int userId, CancellationToken ct = default);

    // Game Rooms
    Task<GameRoom> CreateGameRoomAsync(int tenantId, int hostUserId, string gameType, string mode, int totalRounds, string betType, decimal betAmount, string createdBy, CancellationToken ct = default);
    Task<GameRoom?> GetGameRoomAsync(int tenantId, int roomId, CancellationToken ct = default);
    Task<GameRoom?> GetGameRoomByCodeAsync(int tenantId, string roomCode, CancellationToken ct = default);
    Task<List<GameRoom>> GetActiveGamesAsync(int tenantId, int userId, CancellationToken ct = default);
    Task JoinGameAsync(int tenantId, int roomId, int userId, string createdBy, CancellationToken ct = default);
    Task StartGameAsync(int tenantId, int roomId, int hostUserId, CancellationToken ct = default);

    // Game Play
    Task<object> GetGameQuestionAsync(int tenantId, int roomId, CancellationToken ct = default);
    Task<object> SubmitAnswerAsync(int tenantId, int roomId, int userId, string answer, CancellationToken ct = default);
    Task<object> EndGameAsync(int tenantId, int roomId, CancellationToken ct = default);

    // Stats
    Task<object> GetGameStatsAsync(int tenantId, int userId, CancellationToken ct = default);
}
