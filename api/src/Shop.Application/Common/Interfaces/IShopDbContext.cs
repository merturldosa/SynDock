using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IShopDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Address> Addresses { get; }
    DbSet<Review> Reviews { get; }
    DbSet<QnA> QnAs { get; }
    DbSet<QnAReply> QnAReplies { get; }
    DbSet<Wishlist> Wishlists { get; }
    DbSet<Page> Pages { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostImage> PostImages { get; }
    DbSet<PostComment> PostComments { get; }
    DbSet<PostReaction> PostReactions { get; }
    DbSet<Follow> Follows { get; }
    DbSet<Hashtag> Hashtags { get; }
    DbSet<PostHashtag> PostHashtags { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<UserCoupon> UserCoupons { get; }
    DbSet<UserPoint> UserPoints { get; }
    DbSet<PointHistory> PointHistories { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Saint> Saints { get; }
    DbSet<LiturgicalSeason> LiturgicalSeasons { get; }
    DbSet<OrderHistory> OrderHistories { get; }
    DbSet<Collection> Collections { get; }
    DbSet<CollectionItem> CollectionItems { get; }
    DbSet<TenantPlan> TenantPlans { get; }
    DbSet<ProductDetailSection> ProductDetailSections { get; }
    DbSet<TenantUsage> TenantUsages { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<EmailCampaign> EmailCampaigns { get; }
    DbSet<MesSyncHistory> MesSyncHistories { get; }
    DbSet<ForecastAccuracy> ForecastAccuracies { get; }
    DbSet<CommissionSetting> CommissionSettings { get; }
    DbSet<Commission> Commissions { get; }
    DbSet<Settlement> Settlements { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }
    DbSet<CampaignVariant> CampaignVariants { get; }
    DbSet<CampaignMetric> CampaignMetrics { get; }
    DbSet<ProductionPlanSuggestion> ProductionPlanSuggestions { get; }
    DbSet<SocialPost> SocialPosts { get; }
    DbSet<AutoReorderRule> AutoReorderRules { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<SaintProduct> SaintProducts { get; }
    DbSet<Banner> Banners { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DeliveryDriver> DeliveryDrivers { get; }
    DbSet<DeliveryZone> DeliveryZones { get; }
    DbSet<DeliveryZoneDriver> DeliveryZoneDrivers { get; }
    DbSet<DeliveryOption> DeliveryOptions { get; }
    DbSet<DeliveryAssignment> DeliveryAssignments { get; }
    DbSet<DriverLocationHistory> DriverLocationHistories { get; }

    // WMS
    DbSet<WarehouseZone> WarehouseZones { get; }
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<PickingOrder> PickingOrders { get; }
    DbSet<PickingItem> PickingItems { get; }
    DbSet<PackingSlip> PackingSlips { get; }
    DbSet<BarcodeMapping> BarcodeMappings { get; }
    DbSet<LotTracking> LotTrackings { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptItem> GoodsReceiptItems { get; }
    DbSet<CycleCount> CycleCounts { get; }
    DbSet<CycleCountItem> CycleCountItems { get; }

    // CRM
    DbSet<CustomerSegment> CustomerSegments { get; }
    DbSet<CustomerTag> CustomerTags { get; }
    DbSet<CustomerTagAssignment> CustomerTagAssignments { get; }
    DbSet<CsTicket> CsTickets { get; }
    DbSet<CsTicketMessage> CsTicketMessages { get; }
    DbSet<CustomerJourneyEvent> CustomerJourneyEvents { get; }
    DbSet<LeadScore> LeadScores { get; }
    DbSet<VocEntry> VocEntries { get; }
    DbSet<SalesPipeline> SalesPipelines { get; }

    // ERP
    DbSet<ChartOfAccount> ChartOfAccounts { get; }
    DbSet<AccountEntry> AccountEntries { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<Payroll> Payrolls { get; }
    DbSet<CostAnalysis> CostAnalyses { get; }
    DbSet<AccountingPeriod> AccountingPeriods { get; }

    // SCM
    DbSet<Supplier> Suppliers { get; }
    DbSet<ProcurementOrder> ProcurementOrders { get; }
    DbSet<ProcurementOrderItem> ProcurementOrderItems { get; }
    DbSet<SupplierEvaluation> SupplierEvaluations { get; }

    // Provisioning
    DbSet<TenantApplication> TenantApplications { get; }

    // Migration
    DbSet<MigrationJob> MigrationJobs { get; }

    // PMS (Property Management)
    DbSet<Room> Rooms { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<CleaningTask> CleaningTasks { get; }
    DbSet<RoomAmenityLog> RoomAmenityLogs { get; }

    // Blockchain / Token
    DbSet<BlockchainTransaction> BlockchainTransactions { get; }
    DbSet<TokenWallet> TokenWallets { get; }
    DbSet<TokenTransaction> TokenTransactions { get; }

    // Social Commerce
    DbSet<MemberGrade> MemberGrades { get; }
    DbSet<Gift> Gifts { get; }
    DbSet<ChatRoom> ChatRooms { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    // Friend System + Mini-Game
    DbSet<Friendship> Friendships { get; }
    DbSet<GameRoom> GameRooms { get; }
    DbSet<GamePlayer> GamePlayers { get; }

    // Marketplace
    DbSet<MarketplaceConnection> MarketplaceConnections { get; }
    DbSet<MarketplaceListing> MarketplaceListings { get; }
    DbSet<MarketplaceOrder> MarketplaceOrders { get; }

    // Partner API
    DbSet<ApiPartner> ApiPartners { get; }
    DbSet<PartnerProduct> PartnerProducts { get; }
    DbSet<PartnerApiLog> PartnerApiLogs { get; }

    // Security (AI-SOC)
    DbSet<SecurityEvent> SecurityEvents { get; }
    DbSet<BlockedIp> BlockedIps { get; }
    DbSet<AccountLockout> AccountLockouts { get; }

    // Workflow Engine
    DbSet<WorkItem> WorkItems { get; }
    DbSet<ProcessStep> ProcessSteps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
