using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class FriendGameService : IFriendGameService
{
    private readonly IShopDbContext _db;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<FriendGameService> _logger;
    private static readonly Random _random = new();

    public FriendGameService(IShopDbContext db, IBlockchainService blockchain, ILogger<FriendGameService> logger)
    {
        _db = db;
        _blockchain = blockchain;
        _logger = logger;
    }

    // ───────────────────── Friends ─────────────────────

    public async Task<Friendship> SendFriendRequestAsync(int tenantId, int requesterId, int addresseeId, string createdBy, CancellationToken ct = default)
    {
        if (requesterId == addresseeId)
            throw new InvalidOperationException("자기 자신에게 친구 요청을 보낼 수 없습니다.");

        var existing = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.TenantId == tenantId &&
            ((f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
             (f.RequesterId == addresseeId && f.AddresseeId == requesterId)), ct);

        if (existing != null)
        {
            if (existing.Status == "Accepted") throw new InvalidOperationException("이미 친구입니다.");
            if (existing.Status == "Pending") throw new InvalidOperationException("이미 요청 대기 중입니다.");
            if (existing.Status == "Blocked") throw new InvalidOperationException("차단된 사용자입니다.");
        }

        var friendship = new Friendship
        {
            TenantId = tenantId,
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = "Pending",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.Friendships.Add(friendship);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Friend request sent: {RequesterId} → {AddresseeId} (Tenant {TenantId})", requesterId, addresseeId, tenantId);
        return friendship;
    }

    public async Task AcceptFriendRequestAsync(int tenantId, int friendshipId, int userId, CancellationToken ct = default)
    {
        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.Id == friendshipId && f.TenantId == tenantId && f.AddresseeId == userId && f.Status == "Pending", ct)
            ?? throw new InvalidOperationException("요청을 찾을 수 없습니다.");

        friendship.Status = "Accepted";
        friendship.AcceptedAt = DateTime.UtcNow;
        friendship.UpdatedAt = DateTime.UtcNow;

        // Create notification for requester
        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = friendship.RequesterId,
            Title = "친구 수락",
            Message = "친구 요청이 수락되었습니다.",
            Type = "FriendAccepted",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Friend request accepted: {FriendshipId} by user {UserId}", friendshipId, userId);
    }

    public async Task RemoveFriendAsync(int tenantId, int friendshipId, int userId, CancellationToken ct = default)
    {
        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.Id == friendshipId && f.TenantId == tenantId &&
            (f.RequesterId == userId || f.AddresseeId == userId), ct)
            ?? throw new InvalidOperationException("친구 관계를 찾을 수 없습니다.");

        _db.Friendships.Remove(friendship);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<object>> GetFriendsAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var friendships = await _db.Friendships
            .Where(f => f.TenantId == tenantId && f.Status == "Accepted" &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .ToListAsync(ct);

        return friendships.Select(f =>
        {
            var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
            return (object)new
            {
                FriendshipId = f.Id,
                UserId = friend.Id,
                friend.Username,
                friend.Email,
                AcceptedAt = f.AcceptedAt,
                FriendsSince = f.AcceptedAt
            };
        }).ToList();
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        return await _db.Friendships
            .Where(f => f.TenantId == tenantId && f.AddresseeId == userId && f.Status == "Pending")
            .Include(f => f.Requester)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
    }

    // ───────────────────── Game Rooms ─────────────────────

    public async Task<GameRoom> CreateGameRoomAsync(int tenantId, int hostUserId, string gameType, string mode, int totalRounds, string betType, decimal betAmount, string createdBy, CancellationToken ct = default)
    {
        var validGameTypes = new[] { "WordChain", "TruthGame", "NonsenseQuiz", "HiddenPicture", "JigsawPuzzle", "SpeedQuiz" };
        if (!validGameTypes.Contains(gameType))
            throw new InvalidOperationException($"지원하지 않는 게임 타입: {gameType}");

        var roomCode = GenerateRoomCode();
        var maxPlayers = mode == "1v1" ? 2 : 8;

        var room = new GameRoom
        {
            TenantId = tenantId,
            RoomCode = roomCode,
            GameType = gameType,
            Mode = mode,
            Status = "Waiting",
            HostUserId = hostUserId,
            MaxPlayers = maxPlayers,
            CurrentRound = 0,
            TotalRounds = totalRounds,
            BetType = betType,
            BetAmount = betAmount,
            TotalPot = 0,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.GameRooms.Add(room);
        await _db.SaveChangesAsync(ct);

        // Add host as first player
        var hostPlayer = new GamePlayer
        {
            TenantId = tenantId,
            GameRoomId = room.Id,
            UserId = hostUserId,
            IsReady = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // Handle bet for host
        if (betType != "None" && betAmount > 0)
        {
            await DeductBetAsync(tenantId, hostUserId, betType, betAmount, createdBy, ct);
            hostPlayer.HasBet = true;
            room.TotalPot = betAmount;
        }

        _db.GamePlayers.Add(hostPlayer);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Game room created: {RoomCode} ({GameType}, {Mode}) by user {HostUserId}", roomCode, gameType, mode, hostUserId);
        return room;
    }

    public async Task<GameRoom?> GetGameRoomAsync(int tenantId, int roomId, CancellationToken ct = default)
    {
        return await _db.GameRooms
            .Include(r => r.Players).ThenInclude(p => p.User)
            .Include(r => r.Host)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct);
    }

    public async Task<GameRoom?> GetGameRoomByCodeAsync(int tenantId, string roomCode, CancellationToken ct = default)
    {
        return await _db.GameRooms
            .Include(r => r.Players).ThenInclude(p => p.User)
            .Include(r => r.Host)
            .FirstOrDefaultAsync(r => r.RoomCode == roomCode && r.TenantId == tenantId, ct);
    }

    public async Task<List<GameRoom>> GetActiveGamesAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        return await _db.GameRooms
            .Where(r => r.TenantId == tenantId &&
                        (r.Status == "Waiting" || r.Status == "Playing") &&
                        r.Players.Any(p => p.UserId == userId))
            .Include(r => r.Players).ThenInclude(p => p.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task JoinGameAsync(int tenantId, int roomId, int userId, string createdBy, CancellationToken ct = default)
    {
        var room = await _db.GameRooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("게임 방을 찾을 수 없습니다.");

        if (room.Status != "Waiting")
            throw new InvalidOperationException("이미 시작된 게임입니다.");

        if (room.Players.Count >= room.MaxPlayers)
            throw new InvalidOperationException("방이 가득 찼습니다.");

        if (room.Players.Any(p => p.UserId == userId))
            throw new InvalidOperationException("이미 참가 중입니다.");

        var player = new GamePlayer
        {
            TenantId = tenantId,
            GameRoomId = roomId,
            UserId = userId,
            IsReady = false,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        // Handle bet
        if (room.BetType != "None" && room.BetAmount > 0)
        {
            await DeductBetAsync(tenantId, userId, room.BetType, room.BetAmount, createdBy, ct);
            player.HasBet = true;
            room.TotalPot += room.BetAmount;
        }

        _db.GamePlayers.Add(player);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} joined game room {RoomId}", userId, roomId);
    }

    public async Task StartGameAsync(int tenantId, int roomId, int hostUserId, CancellationToken ct = default)
    {
        var room = await _db.GameRooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("게임 방을 찾을 수 없습니다.");

        if (room.HostUserId != hostUserId)
            throw new InvalidOperationException("호스트만 게임을 시작할 수 있습니다.");

        if (room.Players.Count < 2)
            throw new InvalidOperationException("최소 2명이 필요합니다.");

        if (room.Status != "Waiting")
            throw new InvalidOperationException("게임이 이미 시작되었습니다.");

        // Generate questions based on game type
        var questions = GenerateQuestions(room.GameType, room.TotalRounds);
        room.GameDataJson = JsonSerializer.Serialize(new { questions, currentQuestionIndex = 0 });
        room.Status = "Playing";
        room.CurrentRound = 1;
        room.StartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Game started: room {RoomId} ({GameType})", roomId, room.GameType);
    }

    // ───────────────────── Game Play ─────────────────────

    public async Task<object> GetGameQuestionAsync(int tenantId, int roomId, CancellationToken ct = default)
    {
        var room = await _db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("게임 방을 찾을 수 없습니다.");

        if (room.Status != "Playing")
            throw new InvalidOperationException("게임이 진행 중이 아닙니다.");

        var gameData = JsonSerializer.Deserialize<JsonElement>(room.GameDataJson ?? "{}");
        var questions = gameData.GetProperty("questions");
        var index = gameData.GetProperty("currentQuestionIndex").GetInt32();

        if (index >= questions.GetArrayLength())
            return new { finished = true, message = "모든 문제가 끝났습니다." };

        var question = questions[index];
        return new
        {
            round = room.CurrentRound,
            totalRounds = room.TotalRounds,
            gameType = room.GameType,
            question = question.GetProperty("question").GetString(),
            hint = question.TryGetProperty("hint", out var h) ? h.GetString() : null,
            imageUrl = question.TryGetProperty("imageUrl", out var img) ? img.GetString() : null,
            timeLimit = GetTimeLimitForGameType(room.GameType)
        };
    }

    public async Task<object> SubmitAnswerAsync(int tenantId, int roomId, int userId, string answer, CancellationToken ct = default)
    {
        var room = await _db.GameRooms
            .Include(r => r.Players)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("게임 방을 찾을 수 없습니다.");

        if (room.Status != "Playing")
            throw new InvalidOperationException("게임이 진행 중이 아닙니다.");

        var player = room.Players.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("게임 참가자가 아닙니다.");

        var gameData = JsonSerializer.Deserialize<JsonElement>(room.GameDataJson ?? "{}");
        var questions = gameData.GetProperty("questions");
        var index = gameData.GetProperty("currentQuestionIndex").GetInt32();

        if (index >= questions.GetArrayLength())
            return new { finished = true, message = "모든 문제가 끝났습니다." };

        var question = questions[index];
        var correctAnswer = question.GetProperty("answer").GetString() ?? "";
        var isCorrect = EvaluateAnswer(room.GameType, answer, correctAnswer, question);

        if (isCorrect)
        {
            player.Score += GetScoreForGameType(room.GameType);
            player.CorrectAnswers++;
        }

        // Advance to next question
        var newIndex = index + 1;
        var updatedData = new
        {
            questions = JsonSerializer.Deserialize<JsonElement>(questions.GetRawText()),
            currentQuestionIndex = newIndex
        };
        room.GameDataJson = JsonSerializer.Serialize(updatedData);
        room.CurrentRound = Math.Min(newIndex + 1, room.TotalRounds);

        await _db.SaveChangesAsync(ct);

        var isFinished = newIndex >= questions.GetArrayLength();

        return new
        {
            correct = isCorrect,
            correctAnswer,
            playerScore = player.Score,
            round = room.CurrentRound,
            finished = isFinished
        };
    }

    public async Task<object> EndGameAsync(int tenantId, int roomId, CancellationToken ct = default)
    {
        var room = await _db.GameRooms
            .Include(r => r.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("게임 방을 찾을 수 없습니다.");

        if (room.Status == "Finished")
            throw new InvalidOperationException("이미 종료된 게임입니다.");

        room.Status = "Finished";
        room.FinishedAt = DateTime.UtcNow;

        // Calculate rankings
        var rankedPlayers = room.Players.OrderByDescending(p => p.Score).ThenBy(p => p.CreatedAt).ToList();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            rankedPlayers[i].Rank = i + 1;
        }

        // Distribute pot
        if (room.TotalPot > 0 && rankedPlayers.Count > 0)
        {
            if (room.Mode == "1v1")
            {
                // Winner takes all
                var winner = rankedPlayers[0];
                winner.WinAmount = room.TotalPot;
                await DistributeWinningsAsync(tenantId, winner.UserId, room.BetType, room.TotalPot, "System", ct);
            }
            else
            {
                // 1st: 70%, 2nd: 30%
                var first = rankedPlayers[0];
                var second = rankedPlayers.Count > 1 ? rankedPlayers[1] : null;

                var firstPrize = Math.Round(room.TotalPot * 0.70m, 4);
                var secondPrize = room.TotalPot - firstPrize;

                first.WinAmount = firstPrize;
                await DistributeWinningsAsync(tenantId, first.UserId, room.BetType, firstPrize, "System", ct);

                if (second != null)
                {
                    second.WinAmount = secondPrize;
                    await DistributeWinningsAsync(tenantId, second.UserId, room.BetType, secondPrize, "System", ct);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Game ended: room {RoomId}, winner: {WinnerId}", roomId, rankedPlayers.FirstOrDefault()?.UserId);

        return new
        {
            roomId = room.Id,
            gameType = room.GameType,
            totalPot = room.TotalPot,
            rankings = rankedPlayers.Select(p => new
            {
                rank = p.Rank,
                userId = p.UserId,
                username = p.User?.Username,
                score = p.Score,
                correctAnswers = p.CorrectAnswers,
                winAmount = p.WinAmount
            })
        };
    }

    // ───────────────────── Stats ─────────────────────

    public async Task<object> GetGameStatsAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var players = await _db.GamePlayers
            .Where(p => p.TenantId == tenantId && p.UserId == userId)
            .Include(p => p.GameRoom)
            .Where(p => p.GameRoom.Status == "Finished")
            .ToListAsync(ct);

        var totalGames = players.Count;
        var wins = players.Count(p => p.Rank == 1);
        var totalScore = players.Sum(p => p.Score);
        var totalWinnings = players.Sum(p => p.WinAmount);

        var byGameType = players.GroupBy(p => p.GameRoom.GameType).Select(g => new
        {
            gameType = g.Key,
            played = g.Count(),
            wins = g.Count(p => p.Rank == 1),
            avgScore = g.Average(p => p.Score)
        });

        return new
        {
            totalGames,
            wins,
            losses = totalGames - wins,
            winRate = totalGames > 0 ? Math.Round((double)wins / totalGames * 100, 1) : 0,
            totalScore,
            totalWinnings,
            byGameType
        };
    }

    // ───────────────────── Private Helpers ─────────────────────

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }

    private async Task DeductBetAsync(int tenantId, int userId, string betType, decimal amount, string createdBy, CancellationToken ct)
    {
        switch (betType)
        {
            case "SDT":
                await _blockchain.SpendTokensAsync(tenantId, userId, amount, "게임 배팅", "GameBet", null, createdBy, ct);
                break;
            case "Points":
                var userPoint = await _db.UserPoints.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId, ct);
                if (userPoint == null || userPoint.Balance < (int)amount)
                    throw new InvalidOperationException("포인트가 부족합니다.");
                userPoint.Balance -= (int)amount;
                _db.PointHistories.Add(new PointHistory
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Amount = -(int)amount,
                    TransactionType = "GameBet",
                    Description = "게임 배팅",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                });
                break;
            case "Coupon":
                // Coupon betting: no deduction needed, just mark participation
                break;
        }
    }

    private async Task DistributeWinningsAsync(int tenantId, int userId, string betType, decimal amount, string createdBy, CancellationToken ct)
    {
        switch (betType)
        {
            case "SDT":
                await _blockchain.EarnTokensAsync(tenantId, userId, amount, "GameWin", "게임 승리 보상", "GameRoom", null, createdBy, ct);
                break;
            case "Points":
                var userPoint = await _db.UserPoints.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId, ct);
                if (userPoint != null)
                {
                    userPoint.Balance += (int)amount;
                    _db.PointHistories.Add(new PointHistory
                    {
                        TenantId = tenantId,
                        UserId = userId,
                        Amount = (int)amount,
                        TransactionType = "GameWin",
                        Description = "게임 승리 보상",
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
            case "Coupon":
                // Winner gets a special coupon
                _db.Coupons.Add(new Coupon
                {
                    TenantId = tenantId,
                    Code = $"GAMEWIN-{GenerateRoomCode()}",
                    Name = "게임 승리 쿠폰",
                    Description = "미니게임 승리 보상 쿠폰",
                    DiscountType = "Percentage",
                    DiscountValue = 10,
                    MinOrderAmount = 10000,
                    MaxUsageCount = 1,
                    CurrentUsageCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                });
                break;
        }
    }

    private static List<object> GenerateQuestions(string gameType, int totalRounds)
    {
        return gameType switch
        {
            "NonsenseQuiz" => GenerateNonsenseQuizQuestions(totalRounds),
            "TruthGame" => GenerateTruthGameQuestions(totalRounds),
            "WordChain" => GenerateWordChainQuestions(totalRounds),
            "SpeedQuiz" => GenerateSpeedQuizQuestions(totalRounds),
            "HiddenPicture" => GenerateHiddenPictureQuestions(totalRounds),
            "JigsawPuzzle" => GenerateJigsawPuzzleQuestions(totalRounds),
            _ => GenerateNonsenseQuizQuestions(totalRounds)
        };
    }

    private static List<object> GenerateNonsenseQuizQuestions(int count)
    {
        var all = new List<(string q, string a)>
        {
            ("세상에서 가장 지루한 중학교는?", "하품중학교"),
            ("소가 웃으면?", "우유"),
            ("세상에서 가장 맛없는 집은?", "맛탕"),
            ("왕이 넘어지면?", "킹콩"),
            ("바나나가 웃으면?", "바나나킥"),
            ("오리가 얼면?", "꽥"),
            ("세상에서 가장 가난한 왕은?", "최저임금"),
            ("세상에서 가장 뜨거운 바다는?", "열대바다"),
            ("아몬드가 죽으면?", "다이아몬드"),
            ("불을 가장 무서워하는 동물은?", "소 (소화기)"),
            ("세상에서 가장 빠른 닭은?", "후라이드치킨"),
            ("가장 슬픈 라면은?", "울면"),
            ("소금의 유통기한은?", "천일"),
            ("세상에서 가장 억울한 도형은?", "원"),
            ("가장 게으른 왕은?", "누워왕"),
            ("승리의 반대말은?", "패리"),
            ("네모가 다치면?", "반창고 (네모반창고)"),
            ("가장 무거운 바다는?", "톤해"),
            ("사자가 미용실에 가면?", "파마"),
            ("세상에서 가장 웃긴 과일은?", "배 (배꼽 잡는다)")
        };

        var shuffled = all.OrderBy(_ => _random.Next()).Take(count).ToList();
        return shuffled.Select(q => (object)new { question = q.q, answer = q.a }).ToList();
    }

    private static List<object> GenerateTruthGameQuestions(int count)
    {
        var all = new[]
        {
            "가장 부끄러웠던 순간은?",
            "첫사랑은 언제였나요?",
            "가장 큰 비밀 하나만 말해주세요",
            "무인도에 하나만 가져갈 수 있다면?",
            "10억이 생기면 가장 먼저 할 일은?",
            "가장 존경하는 사람은?",
            "인생 최고의 여행지는?",
            "지금 가장 하고 싶은 것은?",
            "가장 후회되는 일은?",
            "이상형을 설명해주세요",
            "가장 좋아하는 음식은?",
            "최근에 가장 행복했던 순간은?",
            "어릴 때 꿈은 무엇이었나요?",
            "가장 무서웠던 경험은?",
            "요즘 빠져있는 것은?",
            "친구에게 가장 서운했던 적은?",
            "가장 자신 있는 요리는?",
            "스트레스 해소법은?",
            "올해 가장 큰 목표는?",
            "이번 달 가장 큰 지출은?"
        };

        var shuffled = all.OrderBy(_ => _random.Next()).Take(count).ToList();
        // Truth game: answer is free-form, always "correct"
        return shuffled.Select(q => (object)new { question = q, answer = "__TRUTH__", hint = "자유롭게 답해주세요" }).ToList();
    }

    private static List<object> GenerateWordChainQuestions(int count)
    {
        var starters = new[] { "사과", "바나나", "기차", "학교", "고양이", "도서관", "컴퓨터", "음악", "사랑", "화분" };
        var shuffled = starters.OrderBy(_ => _random.Next()).Take(count).ToList();
        return shuffled.Select(w => (object)new
        {
            question = $"'{w}'(으)로 시작하는 끝말잇기! 마지막 글자: '{w[^1]}'",
            answer = w[^1].ToString(), // Last character - player must start with this
            hint = $"'{w[^1]}'(으)로 시작하는 단어를 입력하세요",
            lastChar = w[^1].ToString()
        }).ToList();
    }

    private static List<object> GenerateSpeedQuizQuestions(int count)
    {
        var all = new List<(string q, string a, string h)>
        {
            ("대한민국의 수도는?", "서울", "한강이 흐르는 도시"),
            ("지구에서 가장 큰 바다는?", "태평양", "이름에 '평화'가 들어갑니다"),
            ("한국의 국화는?", "무궁화", "'영원히 피어라'는 뜻"),
            ("세계에서 가장 높은 산은?", "에베레스트", "히말라야 산맥에 있습니다"),
            ("물의 화학식은?", "H2O", "수소 2개 + 산소 1개"),
            ("한글을 만든 왕은?", "세종대왕", "조선 4대 왕"),
            ("태양계에서 가장 큰 행성은?", "목성", "가스 행성"),
            ("대한민국 최초의 한글 소설은?", "홍길동전", "허균이 지었습니다"),
            ("빛의 삼원색은?", "빨강 초록 파랑", "RGB"),
            ("우리나라 최장 강은?", "낙동강", "영남 지방을 흐릅니다"),
            ("세계에서 가장 큰 대륙은?", "아시아", "우리나라가 있는 대륙"),
            ("1년은 몇 주?", "52주", "365일 나누기 7"),
            ("대한민국 국보 1호는?", "숭례문", "남대문이라고도 합니다"),
            ("피타고라스 정리에서 직각삼각형의 빗변 제곱은?", "나머지 두 변의 제곱의 합", "a²+b²=c²"),
            ("커피 원산지 나라는?", "에티오피아", "아프리카 동부")
        };

        var shuffled = all.OrderBy(_ => _random.Next()).Take(count).ToList();
        return shuffled.Select(q => (object)new { question = q.q, answer = q.a, hint = q.h }).ToList();
    }

    private static List<object> GenerateHiddenPictureQuestions(int count)
    {
        // Simulated hidden picture game: describes what to find
        var items = new List<(string desc, string ans)>
        {
            ("숨겨진 별을 찾으세요!", "별"),
            ("그림 속 고양이는 어디 있을까요?", "고양이"),
            ("숨겨진 열쇠를 찾으세요!", "열쇠"),
            ("그림 속 하트를 찾으세요!", "하트"),
            ("숨겨진 음표를 찾으세요!", "음표"),
        };

        var shuffled = items.OrderBy(_ => _random.Next()).Take(count).ToList();
        return shuffled.Select((item, i) => (object)new
        {
            question = item.desc,
            answer = item.ans,
            imageUrl = $"/images/hidden-picture/puzzle-{i + 1}.png",
            hint = $"잘 보세요! {item.ans}이(가) 숨어 있습니다"
        }).ToList();
    }

    private static List<object> GenerateJigsawPuzzleQuestions(int count)
    {
        var puzzles = new[]
        {
            "풍경 퍼즐 - 산과 호수",
            "동물 퍼즐 - 귀여운 강아지",
            "음식 퍼즐 - 한국 전통 음식",
            "도시 퍼즐 - 서울 야경",
            "꽃 퍼즐 - 벚꽃 만개"
        };

        return puzzles.Take(count).Select((p, i) => (object)new
        {
            question = $"퍼즐을 완성하세요: {p}",
            answer = "complete",
            imageUrl = $"/images/jigsaw/puzzle-{i + 1}.png",
            pieces = 9, // 3x3 puzzle
            hint = "조각을 드래그하여 맞춰주세요"
        }).ToList();
    }

    private static bool EvaluateAnswer(string gameType, string userAnswer, string correctAnswer, JsonElement question)
    {
        if (gameType == "TruthGame")
            return !string.IsNullOrWhiteSpace(userAnswer); // Any non-empty answer counts

        if (gameType == "WordChain")
        {
            // User must provide a word starting with the last character
            if (question.TryGetProperty("lastChar", out var lastChar))
            {
                var lc = lastChar.GetString() ?? "";
                return !string.IsNullOrWhiteSpace(userAnswer) && userAnswer.Trim().StartsWith(lc);
            }
            return false;
        }

        if (gameType == "JigsawPuzzle" || gameType == "HiddenPicture")
            return userAnswer.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

        // NonsenseQuiz, SpeedQuiz: check contains (forgiving match)
        var normalizedUser = userAnswer.Trim().Replace(" ", "");
        var normalizedCorrect = correctAnswer.Trim().Replace(" ", "");
        return normalizedUser.Contains(normalizedCorrect, StringComparison.OrdinalIgnoreCase) ||
               normalizedCorrect.Contains(normalizedUser, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetScoreForGameType(string gameType) => gameType switch
    {
        "NonsenseQuiz" => 10,
        "SpeedQuiz" => 15,
        "WordChain" => 5,
        "TruthGame" => 3,
        "HiddenPicture" => 20,
        "JigsawPuzzle" => 25,
        _ => 10
    };

    private static int GetTimeLimitForGameType(string gameType) => gameType switch
    {
        "SpeedQuiz" => 10,
        "NonsenseQuiz" => 15,
        "WordChain" => 10,
        "TruthGame" => 30,
        "HiddenPicture" => 30,
        "JigsawPuzzle" => 60,
        _ => 15
    };
}
