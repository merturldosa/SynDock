using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class SocialCommerceService : ISocialCommerceService
{
    private readonly IShopDbContext _db;
    private readonly IBlockchainService _blockchain;
    private readonly ILogger<SocialCommerceService> _logger;

    public SocialCommerceService(IShopDbContext db, IBlockchainService blockchain, ILogger<SocialCommerceService> logger)
    {
        _db = db;
        _blockchain = blockchain;
        _logger = logger;
    }

    // ===== Member Grades =====

    public async Task<MemberGrade> GetOrCreateGradeAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var grade = await _db.MemberGrades.FirstOrDefaultAsync(g => g.UserId == userId, ct);
        if (grade is not null) return grade;

        grade = new MemberGrade
        {
            TenantId = tenantId,
            UserId = userId,
            Grade = "Bronze",
            GradePoints = 0,
            BonusRate = 0m,
            GradeUpdatedAt = DateTime.UtcNow,
            NextReviewAt = DateTime.UtcNow.AddMonths(1),
            CreatedBy = "system"
        };
        _db.MemberGrades.Add(grade);
        await _db.SaveChangesAsync(ct);
        return grade;
    }

    public async Task<MemberGrade> RecalculateGradeAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var grade = await GetOrCreateGradeAsync(tenantId, userId, ct);

        // Gather stats from DB
        grade.TotalPurchaseAmount = await _db.Orders
            .Where(o => o.UserId == userId && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount ?? 0m, ct);

        grade.TotalOrders = await _db.Orders
            .Where(o => o.UserId == userId && o.Status != "Cancelled")
            .CountAsync(ct);

        grade.TotalReviews = await _db.Reviews
            .Where(r => r.UserId == userId)
            .CountAsync(ct);

        // SDT balance from TokenWallet
        var wallet = await _db.TokenWallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        grade.SdtBalance = wallet?.Balance ?? 0m;

        // Gift counts
        grade.GiftsGiven = await _db.Gifts.Where(g => g.FromUserId == userId).CountAsync(ct);
        grade.GiftsReceived = await _db.Gifts.Where(g => g.ToUserId == userId).CountAsync(ct);

        // Calculate grade points
        var points = (int)(grade.TotalPurchaseAmount / 10000m)
            + (grade.TotalReviews * 50)
            + (grade.TotalReferrals * 100)
            + (int)(grade.SdtBalance * 10m)
            + (grade.GiftsGiven * 30);
        grade.GradePoints = points;

        // Determine grade and bonus rate
        var oldGrade = grade.Grade;
        (grade.Grade, grade.BonusRate) = points switch
        {
            >= 50000 => ("VIP", 15.0m),
            >= 10000 => ("Diamond", 12.0m),
            >= 5000 => ("Platinum", 8.0m),
            >= 2000 => ("Gold", 5.0m),
            >= 500 => ("Silver", 2.0m),
            _ => ("Bronze", 0.0m)
        };

        grade.GradeUpdatedAt = DateTime.UtcNow;
        grade.NextReviewAt = DateTime.UtcNow.AddMonths(1);
        await _db.SaveChangesAsync(ct);

        // If grade upgraded, send SDT reward
        if (oldGrade != grade.Grade && GradeRank(grade.Grade) > GradeRank(oldGrade))
        {
            await HandleGradeUpRewardAsync(tenantId, userId, grade.Grade, ct);
        }

        _logger.LogInformation("Grade recalculated for user {UserId}: {OldGrade} -> {NewGrade} ({Points} pts)",
            userId, oldGrade, grade.Grade, points);

        return grade;
    }

    public async Task<List<MemberGrade>> GetTopMembersAsync(int tenantId, int limit = 20, CancellationToken ct = default)
    {
        return await _db.MemberGrades
            .OrderByDescending(g => g.GradePoints)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task<object> GetGradeBenefitsAsync(string grade, CancellationToken ct = default)
    {
        var benefits = new[]
        {
            new { Grade = "Bronze",   MinPoints = 0,     BonusRate = 0.0,  Monthly = 0,    Perks = "Basic member" },
            new { Grade = "Silver",   MinPoints = 500,   BonusRate = 2.0,  Monthly = 0,    Perks = "Welcome 50 SDT" },
            new { Grade = "Gold",     MinPoints = 2000,  BonusRate = 5.0,  Monthly = 100,  Perks = "Monthly 100 SDT" },
            new { Grade = "Platinum", MinPoints = 5000,  BonusRate = 8.0,  Monthly = 200,  Perks = "Monthly 200 SDT + Birthday gift" },
            new { Grade = "Diamond",  MinPoints = 10000, BonusRate = 12.0, Monthly = 500,  Perks = "Monthly 500 SDT + Exclusive events" },
            new { Grade = "VIP",      MinPoints = 50000, BonusRate = 15.0, Monthly = 1000, Perks = "Monthly 1000 SDT + Personal manager" }
        };

        return Task.FromResult<object>(benefits);
    }

    // ===== Gifts =====

    public async Task<Gift> SendGiftAsync(int tenantId, int fromUserId, int toUserId, string giftType, decimal amount,
        string? message, string? triggerType, int? triggerReferenceId, string createdBy, CancellationToken ct = default)
    {
        if (fromUserId == toUserId)
            throw new InvalidOperationException("Cannot send gift to yourself");

        if (amount <= 0)
            throw new InvalidOperationException("Gift amount must be positive");

        // Validate balance and deduct
        if (giftType == "SDT")
        {
            var wallet = await _db.TokenWallets.FirstOrDefaultAsync(w => w.UserId == fromUserId, ct)
                ?? throw new InvalidOperationException("Sender has no SDT wallet");
            if (wallet.Balance < amount)
                throw new InvalidOperationException($"Insufficient SDT balance. Have {wallet.Balance}, need {amount}");

            // Transfer SDT via blockchain service
            await _blockchain.TransferTokensAsync(tenantId, fromUserId, toUserId, amount,
                $"Gift to user {toUserId}: {message ?? "No message"}", createdBy, ct);
        }
        else if (giftType == "Points")
        {
            var senderPoints = await _db.UserPoints.FirstOrDefaultAsync(p => p.UserId == fromUserId, ct)
                ?? throw new InvalidOperationException("Sender has no points");
            if (senderPoints.Balance < amount)
                throw new InvalidOperationException($"Insufficient points. Have {senderPoints.Balance}, need {amount}");

            senderPoints.Balance -= amount;
            var receiverPoints = await _db.UserPoints.FirstOrDefaultAsync(p => p.UserId == toUserId, ct);
            if (receiverPoints is null)
            {
                receiverPoints = new UserPoint { TenantId = tenantId, UserId = toUserId, Balance = amount, CreatedBy = createdBy };
                _db.UserPoints.Add(receiverPoints);
            }
            else
            {
                receiverPoints.Balance += amount;
            }
        }

        // Create gift record
        var gift = new Gift
        {
            TenantId = tenantId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            GiftType = giftType,
            Amount = amount,
            Message = message,
            TriggerType = triggerType,
            TriggerReferenceId = triggerReferenceId,
            Status = "Sent",
            CreatedBy = createdBy
        };
        _db.Gifts.Add(gift);

        // Create notification for receiver
        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = toUserId,
            Type = "Gift",
            Title = $"You received a {giftType} gift!",
            Message = $"{amount} {giftType} from a member. {message ?? ""}".Trim(),
            ReferenceType = "Gift",
            CreatedBy = createdBy
        });

        // Update member grade gift counts
        var senderGrade = await GetOrCreateGradeAsync(tenantId, fromUserId, ct);
        senderGrade.GiftsGiven++;

        var receiverGrade = await GetOrCreateGradeAsync(tenantId, toUserId, ct);
        receiverGrade.GiftsReceived++;

        await _db.SaveChangesAsync(ct);

        // Auto-create chat room between sender and receiver
        await GetOrCreateChatRoomAsync(tenantId, fromUserId, toUserId, null, null, ct);

        _logger.LogInformation("Gift sent: {GiftType} {Amount} from user {From} to user {To}",
            giftType, amount, fromUserId, toUserId);

        return gift;
    }

    public async Task<List<Gift>> GetGiftsReceivedAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await _db.Gifts
            .Where(g => g.ToUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Gift>> GetGiftsSentAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        return await _db.Gifts
            .Where(g => g.FromUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task ThankGiftAsync(int tenantId, int giftId, int userId, CancellationToken ct = default)
    {
        var gift = await _db.Gifts.FirstOrDefaultAsync(g => g.Id == giftId && g.ToUserId == userId, ct)
            ?? throw new InvalidOperationException("Gift not found or not yours");

        if (gift.Status == "Thanked")
            throw new InvalidOperationException("Gift already thanked");

        gift.Status = "Thanked";
        gift.ThankedAt = DateTime.UtcNow;

        // Notify the gift sender
        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = gift.FromUserId,
            Type = "Gift",
            Title = "Your gift was thanked!",
            Message = $"The recipient thanked you for your {gift.GiftType} gift.",
            ReferenceId = giftId,
            ReferenceType = "Gift",
            CreatedBy = "system"
        });

        await _db.SaveChangesAsync(ct);
    }

    // ===== Chat =====

    public async Task<ChatRoom> GetOrCreateChatRoomAsync(int tenantId, int user1Id, int user2Id, int? productId, int? reviewId, CancellationToken ct = default)
    {
        // Find existing room between these two users (either direction)
        var room = await _db.ChatRooms
            .FirstOrDefaultAsync(r =>
                (r.User1Id == user1Id && r.User2Id == user2Id) ||
                (r.User1Id == user2Id && r.User2Id == user1Id), ct);

        if (room is not null)
        {
            // Update context if provided
            if (productId.HasValue && room.ProductId is null) room.ProductId = productId;
            if (reviewId.HasValue && room.ReviewId is null) room.ReviewId = reviewId;
            if (!room.IsActive) room.IsActive = true;
            await _db.SaveChangesAsync(ct);
            return room;
        }

        room = new ChatRoom
        {
            TenantId = tenantId,
            User1Id = user1Id,
            User2Id = user2Id,
            ProductId = productId,
            ReviewId = reviewId,
            IsActive = true,
            CreatedBy = "system"
        };
        _db.ChatRooms.Add(room);
        await _db.SaveChangesAsync(ct);
        return room;
    }

    public async Task<List<ChatRoom>> GetMyChatRoomsAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        return await _db.ChatRooms
            .Where(r => (r.User1Id == userId || r.User2Id == userId) && r.IsActive)
            .OrderByDescending(r => r.LastMessageAt ?? r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatMessage> SendMessageAsync(int tenantId, int chatRoomId, int senderId, string content,
        string messageType, int? giftId, string createdBy, CancellationToken ct = default)
    {
        var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == chatRoomId, ct)
            ?? throw new InvalidOperationException("Chat room not found");

        // Validate sender is a participant
        if (room.User1Id != senderId && room.User2Id != senderId)
            throw new InvalidOperationException("You are not a participant of this chat room");

        var message = new ChatMessage
        {
            TenantId = tenantId,
            ChatRoomId = chatRoomId,
            SenderId = senderId,
            Content = content,
            MessageType = messageType,
            GiftId = giftId,
            IsRead = false,
            CreatedBy = createdBy
        };
        _db.ChatMessages.Add(message);

        // Update room metadata
        room.LastMessageAt = DateTime.UtcNow;
        room.LastMessagePreview = content.Length > 500 ? content[..497] + "..." : content;

        // Increment unread for the other user
        if (room.User1Id == senderId)
            room.UnreadCount2++;
        else
            room.UnreadCount1++;

        await _db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(int tenantId, int chatRoomId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == chatRoomId, ct)
            ?? throw new InvalidOperationException("Chat room not found");

        if (room.User1Id != userId && room.User2Id != userId)
            throw new InvalidOperationException("You are not a participant of this chat room");

        return await _db.ChatMessages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsReadAsync(int tenantId, int chatRoomId, int userId, CancellationToken ct = default)
    {
        var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == chatRoomId, ct)
            ?? throw new InvalidOperationException("Chat room not found");

        if (room.User1Id != userId && room.User2Id != userId)
            throw new InvalidOperationException("You are not a participant of this chat room");

        // Mark all unread messages from the other user as read
        var unreadMessages = await _db.ChatMessages
            .Where(m => m.ChatRoomId == chatRoomId && m.SenderId != userId && !m.IsRead)
            .ToListAsync(ct);

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        // Reset unread counter
        if (room.User1Id == userId)
            room.UnreadCount1 = 0;
        else
            room.UnreadCount2 = 0;

        await _db.SaveChangesAsync(ct);
    }

    // ===== Private Helpers =====

    private async Task HandleGradeUpRewardAsync(int tenantId, int userId, string newGrade, CancellationToken ct)
    {
        var sdtReward = newGrade switch
        {
            "Silver" => 50m,
            "Gold" => 100m,
            "Platinum" => 200m,
            "Diamond" => 500m,
            "VIP" => 1000m,
            _ => 0m
        };

        if (sdtReward > 0)
        {
            await _blockchain.EarnTokensAsync(tenantId, userId, sdtReward, "GradeUpReward",
                $"Grade upgrade reward: {newGrade}", "MemberGrade", null, "system", ct);
        }

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = "Grade",
            Title = $"Congratulations! You are now {newGrade}!",
            Message = sdtReward > 0
                ? $"You've been upgraded to {newGrade} grade and earned {sdtReward} SDT!"
                : $"You've been upgraded to {newGrade} grade!",
            ReferenceType = "MemberGrade",
            CreatedBy = "system"
        });

        await _db.SaveChangesAsync(ct);
    }

    private static int GradeRank(string grade) => grade switch
    {
        "Bronze" => 0,
        "Silver" => 1,
        "Gold" => 2,
        "Platinum" => 3,
        "Diamond" => 4,
        "VIP" => 5,
        _ => 0
    };
}
