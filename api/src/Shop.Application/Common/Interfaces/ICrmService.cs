using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface ICrmService
{
    // Customer Segments
    Task<List<CustomerSegment>> GetSegmentsAsync(int tenantId, CancellationToken ct = default);
    Task<CustomerSegment> CreateSegmentAsync(int tenantId, string name, string type, string? description, string? rulesJson, string createdBy, CancellationToken ct = default);
    Task RecalculateSegmentAsync(int tenantId, int segmentId, CancellationToken ct = default);

    // Customer Tags
    Task<List<CustomerTag>> GetTagsAsync(int tenantId, CancellationToken ct = default);
    Task<CustomerTag> CreateTagAsync(int tenantId, string name, string color, string? description, string createdBy, CancellationToken ct = default);
    Task AssignTagAsync(int tenantId, int userId, int tagId, string createdBy, CancellationToken ct = default);
    Task RemoveTagAsync(int tenantId, int userId, int tagId, CancellationToken ct = default);
    Task<List<CustomerTagAssignment>> GetUserTagsAsync(int tenantId, int userId, CancellationToken ct = default);

    // CS Tickets
    Task<CsTicket> CreateTicketAsync(int tenantId, int userId, string subject, string category, string priority, string content, int? orderId, string createdBy, CancellationToken ct = default);
    Task<CsTicket?> GetTicketAsync(int tenantId, int ticketId, CancellationToken ct = default);
    Task<(List<CsTicket> Items, int TotalCount)> GetTicketsAsync(int tenantId, string? status, string? category, int? userId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<CsTicketMessage> ReplyToTicketAsync(int tenantId, int ticketId, int senderId, string senderType, string content, string? attachmentUrl, bool isInternal, string createdBy, CancellationToken ct = default);
    Task UpdateTicketStatusAsync(int tenantId, int ticketId, string status, int? assignedToUserId, string updatedBy, CancellationToken ct = default);
    Task RateTicketAsync(int tenantId, int ticketId, int rating, string? comment, CancellationToken ct = default);

    // Customer Journey
    Task TrackEventAsync(int tenantId, int userId, string eventType, string? eventDetail, int? referenceId, string? referenceType, string? metadataJson, string? channel, string? sessionId, CancellationToken ct = default);
    Task<List<CustomerJourneyEvent>> GetCustomerJourneyAsync(int tenantId, int userId, int limit = 100, CancellationToken ct = default);

    // Analytics
    Task<object> GetCustomerAnalyticsAsync(int tenantId, CancellationToken ct = default);
    Task<object> GetTicketAnalyticsAsync(int tenantId, DateTime? from, DateTime? to, CancellationToken ct = default);

    // Customer 360 View
    Task<object> GetCustomer360Async(int tenantId, int userId, CancellationToken ct = default);

    // Lead Scoring
    Task<LeadScore> CalculateLeadScoreAsync(int tenantId, int userId, CancellationToken ct = default);
    Task RecalculateAllLeadScoresAsync(int tenantId, CancellationToken ct = default);
    Task<List<LeadScore>> GetTopLeadsAsync(int tenantId, int limit = 50, CancellationToken ct = default);

    // VoC
    Task<VocEntry> CreateVocEntryAsync(int tenantId, int? userId, string source, int? sourceId, string content, string createdBy, CancellationToken ct = default);
    Task<List<VocEntry>> GetVocEntriesAsync(int tenantId, string? sentiment = null, string? source = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<object> GetVocDashboardAsync(int tenantId, CancellationToken ct = default);

    // Sales Pipeline
    Task<SalesPipeline> CreatePipelineAsync(int tenantId, int? userId, string title, decimal expectedValue, DateTime? expectedCloseDate, string? assignedTo, string createdBy, CancellationToken ct = default);
    Task<List<SalesPipeline>> GetPipelinesAsync(int tenantId, string? stage = null, CancellationToken ct = default);
    Task UpdatePipelineStageAsync(int tenantId, int pipelineId, string stage, string? notes, string updatedBy, CancellationToken ct = default);
    Task<object> GetPipelineDashboardAsync(int tenantId, CancellationToken ct = default);
}
