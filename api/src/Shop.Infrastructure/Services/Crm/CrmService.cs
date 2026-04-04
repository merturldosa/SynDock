using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class CrmService : ICrmService
{
    private readonly IShopDbContext _db;

    public CrmService(IShopDbContext db) => _db = db;

    // Customer Segments
    public async Task<List<CustomerSegment>> GetSegmentsAsync(int tenantId, CancellationToken ct = default)
        => await _db.CustomerSegments.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<CustomerSegment> CreateSegmentAsync(int tenantId, string name, string type, string? description, string? rulesJson, string createdBy, CancellationToken ct = default)
    {
        var segment = new CustomerSegment { TenantId = tenantId, Name = name, Type = type, Description = description, RulesJson = rulesJson, CreatedBy = createdBy };
        _db.CustomerSegments.Add(segment);
        await _db.SaveChangesAsync(ct);
        return segment;
    }

    public async Task RecalculateSegmentAsync(int tenantId, int segmentId, CancellationToken ct = default)
    {
        var segment = await _db.CustomerSegments.FirstOrDefaultAsync(s => s.Id == segmentId, ct) ?? throw new InvalidOperationException("Segment not found");
        // Clear existing assignments
        var existing = await _db.CustomerTagAssignments.Where(a => a.CustomerSegmentId == segmentId).ToListAsync(ct);
        foreach (var a in existing) _db.CustomerTagAssignments.Remove(a);

        // For dynamic segments, apply rules (simplified: count all active users)
        var users = await _db.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync(ct);
        foreach (var userId in users)
        {
            _db.CustomerTagAssignments.Add(new CustomerTagAssignment { TenantId = tenantId, UserId = userId, CustomerSegmentId = segmentId, CreatedBy = "system" });
        }
        segment.MemberCount = users.Count;
        segment.LastCalculatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // Customer Tags
    public async Task<List<CustomerTag>> GetTagsAsync(int tenantId, CancellationToken ct = default)
        => await _db.CustomerTags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<CustomerTag> CreateTagAsync(int tenantId, string name, string color, string? description, string createdBy, CancellationToken ct = default)
    {
        var tag = new CustomerTag { TenantId = tenantId, Name = name, Color = color, Description = description, CreatedBy = createdBy };
        _db.CustomerTags.Add(tag);
        await _db.SaveChangesAsync(ct);
        return tag;
    }

    public async Task AssignTagAsync(int tenantId, int userId, int tagId, string createdBy, CancellationToken ct = default)
    {
        var exists = await _db.CustomerTagAssignments.AnyAsync(a => a.UserId == userId && a.CustomerTagId == tagId, ct);
        if (!exists)
        {
            _db.CustomerTagAssignments.Add(new CustomerTagAssignment { TenantId = tenantId, UserId = userId, CustomerTagId = tagId, CreatedBy = createdBy });
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTagAsync(int tenantId, int userId, int tagId, CancellationToken ct = default)
    {
        var assignment = await _db.CustomerTagAssignments.FirstOrDefaultAsync(a => a.UserId == userId && a.CustomerTagId == tagId, ct);
        if (assignment != null) { _db.CustomerTagAssignments.Remove(assignment); await _db.SaveChangesAsync(ct); }
    }

    public async Task<List<CustomerTagAssignment>> GetUserTagsAsync(int tenantId, int userId, CancellationToken ct = default)
        => await _db.CustomerTagAssignments.AsNoTracking().Include(a => a.Tag).Include(a => a.Segment).Where(a => a.UserId == userId).ToListAsync(ct);

    // CS Tickets
    public async Task<CsTicket> CreateTicketAsync(int tenantId, int userId, string subject, string category, string priority, string content, int? orderId, string createdBy, CancellationToken ct = default)
    {
        var ticketNumber = $"CS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var ticket = new CsTicket
        {
            TenantId = tenantId, TicketNumber = ticketNumber, UserId = userId,
            Subject = subject, Category = category, Priority = priority, OrderId = orderId, CreatedBy = createdBy
        };
        _db.CsTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        // Add first message
        _db.CsTicketMessages.Add(new CsTicketMessage
        {
            TenantId = tenantId, CsTicketId = ticket.Id, SenderId = userId,
            SenderType = "Customer", Content = content, CreatedBy = createdBy
        });
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<CsTicket?> GetTicketAsync(int tenantId, int ticketId, CancellationToken ct = default)
        => await _db.CsTickets.AsNoTracking().Include(t => t.Messages.OrderBy(m => m.CreatedAt)).ThenInclude(m => m.Sender).Include(t => t.User).Include(t => t.AssignedToUser).FirstOrDefaultAsync(t => t.Id == ticketId, ct);

    public async Task<(List<CsTicket> Items, int TotalCount)> GetTicketsAsync(int tenantId, string? status, string? category, int? userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.CsTickets.AsNoTracking().Include(t => t.User).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(category)) query = query.Where(t => t.Category == category);
        if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<CsTicketMessage> ReplyToTicketAsync(int tenantId, int ticketId, int senderId, string senderType, string content, string? attachmentUrl, bool isInternal, string createdBy, CancellationToken ct = default)
    {
        var ticket = await _db.CsTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        if (senderType == "Agent" && ticket.FirstResponseAt == null)
        {
            ticket.FirstResponseAt = DateTime.UtcNow;
        }
        if (ticket.Status == "Open") ticket.Status = "InProgress";

        var message = new CsTicketMessage
        {
            TenantId = tenantId, CsTicketId = ticketId, SenderId = senderId,
            SenderType = senderType, Content = content, AttachmentUrl = attachmentUrl,
            IsInternal = isInternal, CreatedBy = createdBy
        };
        _db.CsTicketMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    public async Task UpdateTicketStatusAsync(int tenantId, int ticketId, string status, int? assignedToUserId, string updatedBy, CancellationToken ct = default)
    {
        var ticket = await _db.CsTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.Status = status;
        if (assignedToUserId.HasValue) ticket.AssignedToUserId = assignedToUserId;
        if (status == "Resolved") ticket.ResolvedAt = DateTime.UtcNow;
        if (status == "Closed") ticket.ClosedAt = DateTime.UtcNow;
        ticket.UpdatedBy = updatedBy;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RateTicketAsync(int tenantId, int ticketId, int rating, string? comment, CancellationToken ct = default)
    {
        var ticket = await _db.CsTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct) ?? throw new InvalidOperationException("Ticket not found");
        ticket.SatisfactionRating = rating;
        ticket.SatisfactionComment = comment;
        await _db.SaveChangesAsync(ct);
    }

    // Customer Journey
    public async Task TrackEventAsync(int tenantId, int userId, string eventType, string? eventDetail, int? referenceId, string? referenceType, string? metadataJson, string? channel, string? sessionId, CancellationToken ct = default)
    {
        _db.CustomerJourneyEvents.Add(new CustomerJourneyEvent
        {
            TenantId = tenantId, UserId = userId, EventType = eventType,
            EventDetail = eventDetail, ReferenceId = referenceId, ReferenceType = referenceType,
            MetadataJson = metadataJson, Channel = channel, SessionId = sessionId, CreatedBy = "system"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CustomerJourneyEvent>> GetCustomerJourneyAsync(int tenantId, int userId, int limit = 100, CancellationToken ct = default)
        => await _db.CustomerJourneyEvents.AsNoTracking().Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync(ct);

    // Analytics
    public async Task<object> GetCustomerAnalyticsAsync(int tenantId, CancellationToken ct = default)
    {
        var totalCustomers = await _db.Users.CountAsync(u => u.Role == "Member", ct);
        var activeCustomers = await _db.Orders.Select(o => o.UserId).Distinct().CountAsync(ct);
        var segmentCounts = await _db.CustomerSegments.Select(s => new { s.Name, s.MemberCount }).ToListAsync(ct);
        var tagCounts = await _db.CustomerTagAssignments.Where(a => a.CustomerTagId != null).GroupBy(a => a.Tag!.Name).Select(g => new { Tag = g.Key, Count = g.Count() }).ToListAsync(ct);

        return new { totalCustomers, activeCustomers, segments = segmentCounts, tags = tagCounts };
    }

    public async Task<object> GetTicketAnalyticsAsync(int tenantId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.CsTickets.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var byStatus = await query.GroupBy(t => t.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var byCategory = await query.GroupBy(t => t.Category).Select(g => new { Category = g.Key, Count = g.Count() }).ToListAsync(ct);
        var avgRating = await query.Where(t => t.SatisfactionRating != null).AverageAsync(t => (double?)t.SatisfactionRating, ct) ?? 0;

        return new { total, byStatus, byCategory, avgSatisfaction = Math.Round(avgRating, 1) };
    }

    // Customer 360 View
    public async Task<object> GetCustomer360Async(int tenantId, int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found");

        // Order summary
        var orders = await _db.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync(ct);
        var totalSpent = orders.Sum(o => o.TotalAmount);
        var avgOrderValue = orders.Count > 0 ? totalSpent / orders.Count : 0m;
        var lastOrderDate = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt;

        // Recent orders (last 10)
        var recentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(10)
            .Select(o => new { o.Id, o.OrderNumber, o.TotalAmount, o.Status, o.CreatedAt }).ToList();

        // Tags & segments
        var tags = await _db.CustomerTagAssignments.AsNoTracking()
            .Include(a => a.Tag).Include(a => a.Segment)
            .Where(a => a.UserId == userId).ToListAsync(ct);

        // Lead score
        var leadScore = await _db.LeadScores.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId, ct);

        // Recent journey events
        var journeyEvents = await _db.CustomerJourneyEvents.AsNoTracking()
            .Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedAt).Take(20).ToListAsync(ct);

        // Open CS tickets
        var openTickets = await _db.CsTickets.AsNoTracking()
            .Where(t => t.UserId == userId && t.Status != "Closed" && t.Status != "Resolved")
            .OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

        // Reviews
        var reviews = await _db.Reviews.AsNoTracking().Where(r => r.UserId == userId).ToListAsync(ct);
        var reviewCount = reviews.Count;
        var avgRating = reviewCount > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;

        // Points balance
        var points = await _db.UserPoints.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        var pointsBalance = points?.Balance ?? 0;

        // Active coupons
        var activeCoupons = await _db.UserCoupons.AsNoTracking()
            .Include(c => c.Coupon)
            .Where(c => c.UserId == userId && !c.IsUsed && c.Coupon.EndDate > DateTime.UtcNow)
            .CountAsync(ct);

        return new
        {
            user = new { user.Id, user.Username, user.Email, user.Role, joinDate = user.CreatedAt },
            orderSummary = new { totalOrders = orders.Count, totalSpent, avgOrderValue, lastOrderDate },
            recentOrders,
            tags = tags.Select(t => new { tagName = t.Tag?.Name, segmentName = t.Segment?.Name }),
            leadScore = leadScore != null ? new { leadScore.TotalScore, leadScore.Grade, leadScore.PurchaseScore, leadScore.EngagementScore, leadScore.RecencyScore, leadScore.FrequencyScore, leadScore.LastCalculatedAt } : null,
            recentJourney = journeyEvents.Select(e => new { e.EventType, e.EventDetail, e.Channel, e.CreatedAt }),
            openTickets = openTickets.Select(t => new { t.Id, t.TicketNumber, t.Subject, t.Status, t.Priority, t.CreatedAt }),
            reviews = new { count = reviewCount, avgRating },
            pointsBalance,
            activeCoupons
        };
    }

    // Lead Scoring
    public async Task<LeadScore> CalculateLeadScoreAsync(int tenantId, int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // PurchaseScore (0-30): based on total spent
        var orders = await _db.Orders.AsNoTracking().Where(o => o.UserId == userId).ToListAsync(ct);
        var totalSpent = orders.Sum(o => o.TotalAmount);
        var purchaseScore = totalSpent switch
        {
            > 500_000m => 30,
            > 200_000m => 20,
            > 50_000m => 10,
            _ => 5
        };

        // EngagementScore (0-25): journey events in last 30 days
        var thirtyDaysAgo = now.AddDays(-30);
        var eventCount = await _db.CustomerJourneyEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.CreatedAt >= thirtyDaysAgo).CountAsync(ct);
        var engagementScore = eventCount switch
        {
            > 50 => 25,
            > 30 => 20,
            > 10 => 15,
            > 3 => 10,
            _ => 5
        };

        // RecencyScore (0-25): days since last order
        var lastOrderDate = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt;
        var daysSinceLastOrder = lastOrderDate.HasValue ? (now - lastOrderDate.Value).TotalDays : 999;
        var recencyScore = daysSinceLastOrder switch
        {
            < 7 => 25,
            < 30 => 20,
            < 90 => 10,
            _ => 5
        };

        // FrequencyScore (0-20): orders per month (over last 6 months)
        var sixMonthsAgo = now.AddMonths(-6);
        var recentOrderCount = orders.Count(o => o.CreatedAt >= sixMonthsAgo);
        var ordersPerMonth = recentOrderCount / 6.0;
        var frequencyScore = ordersPerMonth switch
        {
            > 4 => 20,
            > 2 => 15,
            > 1 => 10,
            > 0.3 => 5,
            _ => 0
        };

        var totalScore = purchaseScore + engagementScore + recencyScore + frequencyScore;
        var grade = totalScore switch
        {
            >= 80 => "A",
            >= 60 => "B",
            >= 40 => "C",
            >= 20 => "D",
            _ => "F"
        };

        // Upsert
        var existing = await _db.LeadScores.FirstOrDefaultAsync(l => l.UserId == userId && l.TenantId == tenantId, ct);
        if (existing == null)
        {
            existing = new LeadScore { TenantId = tenantId, UserId = userId, CreatedBy = "system" };
            _db.LeadScores.Add(existing);
        }

        existing.PurchaseScore = purchaseScore;
        existing.EngagementScore = engagementScore;
        existing.RecencyScore = recencyScore;
        existing.FrequencyScore = frequencyScore;
        existing.TotalScore = totalScore;
        existing.Grade = grade;
        existing.LastCalculatedAt = now;
        existing.ScoreBreakdownJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            totalSpent,
            eventCount,
            daysSinceLastOrder = Math.Round(daysSinceLastOrder, 1),
            ordersPerMonth = Math.Round(ordersPerMonth, 2),
            recentOrderCount
        });

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task RecalculateAllLeadScoresAsync(int tenantId, CancellationToken ct = default)
    {
        var userIds = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role == "Member")
            .Select(u => u.Id).ToListAsync(ct);

        foreach (var userId in userIds)
        {
            await CalculateLeadScoreAsync(tenantId, userId, ct);
        }
    }

    public async Task<List<LeadScore>> GetTopLeadsAsync(int tenantId, int limit = 50, CancellationToken ct = default)
        => await _db.LeadScores.AsNoTracking().Include(l => l.User)
            .OrderByDescending(l => l.TotalScore).Take(limit).ToListAsync(ct);

    // VoC
    public async Task<VocEntry> CreateVocEntryAsync(int tenantId, int? userId, string source, int? sourceId, string content, string createdBy, CancellationToken ct = default)
    {
        // Simple keyword-based sentiment analysis
        var lowerContent = content.ToLowerInvariant();
        var positiveWords = new[] { "great", "good", "excellent", "love", "perfect", "amazing", "best", "fantastic", "recommend", "satisfied", "좋", "만족", "최고", "추천", "훌륭" };
        var negativeWords = new[] { "bad", "poor", "terrible", "awful", "worst", "hate", "disappointed", "broken", "refund", "complaint", "나쁘", "불만", "최악", "환불", "불량" };

        var posCount = positiveWords.Count(w => lowerContent.Contains(w));
        var negCount = negativeWords.Count(w => lowerContent.Contains(w));

        var sentiment = posCount > negCount ? "Positive" : negCount > posCount ? "Negative" : "Neutral";
        var sentimentScore = posCount + negCount == 0 ? 0m : Math.Round((decimal)(posCount - negCount) / (posCount + negCount), 2);

        // Simple topic detection
        var topicKeywords = new Dictionary<string, string[]>
        {
            ["Product Quality"] = new[] { "quality", "defect", "material", "품질", "불량", "소재" },
            ["Shipping"] = new[] { "shipping", "delivery", "arrived", "배송", "택배", "도착" },
            ["Service"] = new[] { "service", "support", "response", "서비스", "응대", "답변" },
            ["Price"] = new[] { "price", "expensive", "cheap", "가격", "비싸", "저렴" }
        };
        var topic = topicKeywords.FirstOrDefault(kv => kv.Value.Any(w => lowerContent.Contains(w))).Key;

        var entry = new VocEntry
        {
            TenantId = tenantId, UserId = userId, Source = source, SourceId = sourceId,
            Content = content, Sentiment = sentiment, SentimentScore = sentimentScore,
            TopicCategory = topic, CreatedBy = createdBy
        };
        _db.VocEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<List<VocEntry>> GetVocEntriesAsync(int tenantId, string? sentiment = null, string? source = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.VocEntries.AsNoTracking().Include(v => v.User).AsQueryable();
        if (!string.IsNullOrEmpty(sentiment)) query = query.Where(v => v.Sentiment == sentiment);
        if (!string.IsNullOrEmpty(source)) query = query.Where(v => v.Source == source);
        return await query.OrderByDescending(v => v.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<object> GetVocDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var entries = await _db.VocEntries.AsNoTracking().ToListAsync(ct);

        // Sentiment distribution
        var sentimentDist = entries.GroupBy(e => e.Sentiment)
            .Select(g => new { sentiment = g.Key, count = g.Count() }).ToList();

        // Top topics
        var topTopics = entries.Where(e => e.TopicCategory != null)
            .GroupBy(e => e.TopicCategory!)
            .Select(g => new { topic = g.Key, count = g.Count(), avgScore = Math.Round(g.Average(e => (double)e.SentimentScore), 2) })
            .OrderByDescending(t => t.count).ToList();

        // Trend over time (last 12 months)
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var trend = entries.Where(e => e.CreatedAt >= twelveMonthsAgo)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                month = $"{g.Key.Year}-{g.Key.Month:D2}",
                total = g.Count(),
                positive = g.Count(e => e.Sentiment == "Positive"),
                negative = g.Count(e => e.Sentiment == "Negative"),
                neutral = g.Count(e => e.Sentiment == "Neutral")
            }).ToList();

        // Source distribution
        var sourceDist = entries.GroupBy(e => e.Source)
            .Select(g => new { source = g.Key, count = g.Count() }).ToList();

        return new
        {
            totalEntries = entries.Count,
            sentimentDistribution = sentimentDist,
            topTopics,
            trend,
            sourceDistribution = sourceDist,
            avgSentimentScore = entries.Count > 0 ? Math.Round(entries.Average(e => (double)e.SentimentScore), 2) : 0
        };
    }

    // Sales Pipeline
    public async Task<SalesPipeline> CreatePipelineAsync(int tenantId, int? userId, string title, decimal expectedValue, DateTime? expectedCloseDate, string? assignedTo, string createdBy, CancellationToken ct = default)
    {
        var pipeline = new SalesPipeline
        {
            TenantId = tenantId, UserId = userId, Title = title,
            ExpectedValue = expectedValue, Probability = 10,
            ExpectedCloseDate = expectedCloseDate, AssignedTo = assignedTo, CreatedBy = createdBy
        };
        _db.SalesPipelines.Add(pipeline);
        await _db.SaveChangesAsync(ct);
        return pipeline;
    }

    public async Task<List<SalesPipeline>> GetPipelinesAsync(int tenantId, string? stage = null, CancellationToken ct = default)
    {
        var query = _db.SalesPipelines.AsNoTracking().Include(p => p.User).AsQueryable();
        if (!string.IsNullOrEmpty(stage)) query = query.Where(p => p.Stage == stage);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task UpdatePipelineStageAsync(int tenantId, int pipelineId, string stage, string? notes, string updatedBy, CancellationToken ct = default)
    {
        var pipeline = await _db.SalesPipelines.FirstOrDefaultAsync(p => p.Id == pipelineId, ct)
            ?? throw new InvalidOperationException("Pipeline not found");

        pipeline.Stage = stage;
        if (notes != null) pipeline.Notes = notes;
        pipeline.UpdatedBy = updatedBy;
        pipeline.UpdatedAt = DateTime.UtcNow;

        // Update probability based on stage
        pipeline.Probability = stage switch
        {
            "Lead" => 10,
            "Contacted" => 20,
            "Qualified" => 40,
            "Proposal" => 60,
            "Negotiation" => 80,
            "Won" => 100,
            "Lost" => 0,
            _ => pipeline.Probability
        };

        if (stage == "Won") pipeline.WonAt = DateTime.UtcNow;
        if (stage == "Lost") { pipeline.LostAt = DateTime.UtcNow; pipeline.LostReason = notes; }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetPipelineDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var pipelines = await _db.SalesPipelines.AsNoTracking().ToListAsync(ct);

        var stages = new[] { "Lead", "Contacted", "Qualified", "Proposal", "Negotiation", "Won", "Lost" };
        var funnel = stages.Select(s =>
        {
            var items = pipelines.Where(p => p.Stage == s).ToList();
            return new { stage = s, count = items.Count, totalValue = items.Sum(p => p.ExpectedValue) };
        }).ToList();

        var totalExpectedValue = pipelines.Where(p => p.Stage != "Lost" && p.Stage != "Won").Sum(p => p.ExpectedValue);
        var weightedValue = pipelines.Where(p => p.Stage != "Lost" && p.Stage != "Won").Sum(p => p.ExpectedValue * p.Probability / 100);

        var wonCount = pipelines.Count(p => p.Stage == "Won");
        var lostCount = pipelines.Count(p => p.Stage == "Lost");
        var winRate = wonCount + lostCount > 0 ? Math.Round((double)wonCount / (wonCount + lostCount) * 100, 1) : 0;

        var avgDealSize = pipelines.Where(p => p.Stage == "Won").Select(p => p.ExpectedValue).DefaultIfEmpty(0).Average();

        return new
        {
            funnel,
            totalExpectedValue,
            weightedValue,
            winRate,
            avgDealSize,
            totalDeals = pipelines.Count,
            openDeals = pipelines.Count(p => p.Stage != "Won" && p.Stage != "Lost")
        };
    }
}
