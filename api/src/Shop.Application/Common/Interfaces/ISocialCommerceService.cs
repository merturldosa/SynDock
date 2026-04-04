using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface ISocialCommerceService
{
    // Member Grades
    Task<MemberGrade> GetOrCreateGradeAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<MemberGrade> RecalculateGradeAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<List<MemberGrade>> GetTopMembersAsync(int tenantId, int limit = 20, CancellationToken ct = default);
    Task<object> GetGradeBenefitsAsync(string grade, CancellationToken ct = default);

    // Gifts
    Task<Gift> SendGiftAsync(int tenantId, int fromUserId, int toUserId, string giftType, decimal amount, string? message, string? triggerType, int? triggerReferenceId, string createdBy, CancellationToken ct = default);
    Task<List<Gift>> GetGiftsReceivedAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<List<Gift>> GetGiftsSentAsync(int tenantId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task ThankGiftAsync(int tenantId, int giftId, int userId, CancellationToken ct = default);

    // Chat
    Task<ChatRoom> GetOrCreateChatRoomAsync(int tenantId, int user1Id, int user2Id, int? productId, int? reviewId, CancellationToken ct = default);
    Task<List<ChatRoom>> GetMyChatRoomsAsync(int tenantId, int userId, CancellationToken ct = default);
    Task<ChatMessage> SendMessageAsync(int tenantId, int chatRoomId, int senderId, string content, string messageType, int? giftId, string createdBy, CancellationToken ct = default);
    Task<List<ChatMessage>> GetMessagesAsync(int tenantId, int chatRoomId, int userId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task MarkAsReadAsync(int tenantId, int chatRoomId, int userId, CancellationToken ct = default);
}
