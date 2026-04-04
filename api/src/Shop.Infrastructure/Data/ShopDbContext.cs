using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Data;

public class ShopDbContext : DbContext, IShopDbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IEncryptionService? _encryptionService;

    public ShopDbContext(DbContextOptions<ShopDbContext> options, ITenantContext tenantContext,
        IEncryptionService? encryptionService = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _encryptionService = encryptionService;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<QnA> QnAs { get; set; }
    public DbSet<QnAReply> QnAReplies { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostImage> PostImages { get; set; }
    public DbSet<PostComment> PostComments { get; set; }
    public DbSet<PostReaction> PostReactions { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<PostHashtag> PostHashtags { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<UserCoupon> UserCoupons { get; set; }
    public DbSet<UserPoint> UserPoints { get; set; }
    public DbSet<PointHistory> PointHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Saint> Saints { get; set; }
    public DbSet<LiturgicalSeason> LiturgicalSeasons { get; set; }
    public DbSet<OrderHistory> OrderHistories { get; set; }
    public DbSet<Collection> Collections { get; set; }
    public DbSet<CollectionItem> CollectionItems { get; set; }
    public DbSet<TenantPlan> TenantPlans { get; set; }
    public DbSet<ProductDetailSection> ProductDetailSections { get; set; }
    public DbSet<TenantUsage> TenantUsages { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<EmailCampaign> EmailCampaigns { get; set; }
    public DbSet<MesSyncHistory> MesSyncHistories { get; set; }
    public DbSet<ForecastAccuracy> ForecastAccuracies { get; set; }
    public DbSet<CommissionSetting> CommissionSettings { get; set; }
    public DbSet<Commission> Commissions { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<PushSubscription> PushSubscriptions { get; set; }
    public DbSet<CampaignVariant> CampaignVariants { get; set; }
    public DbSet<CampaignMetric> CampaignMetrics { get; set; }
    public DbSet<ProductionPlanSuggestion> ProductionPlanSuggestions { get; set; }
    public DbSet<SocialPost> SocialPosts { get; set; }
    public DbSet<AutoReorderRule> AutoReorderRules { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<SaintProduct> SaintProducts { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<DeliveryDriver> DeliveryDrivers { get; set; }
    public DbSet<DeliveryZone> DeliveryZones { get; set; }
    public DbSet<DeliveryZoneDriver> DeliveryZoneDrivers { get; set; }
    public DbSet<DeliveryOption> DeliveryOptions { get; set; }
    public DbSet<DeliveryAssignment> DeliveryAssignments { get; set; }
    public DbSet<DriverLocationHistory> DriverLocationHistories { get; set; }

    // WMS
    public DbSet<WarehouseZone> WarehouseZones { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<PickingOrder> PickingOrders { get; set; }
    public DbSet<PickingItem> PickingItems { get; set; }
    public DbSet<PackingSlip> PackingSlips { get; set; }
    public DbSet<BarcodeMapping> BarcodeMappings { get; set; }
    public DbSet<LotTracking> LotTrackings { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }
    public DbSet<CycleCount> CycleCounts { get; set; }
    public DbSet<CycleCountItem> CycleCountItems { get; set; }

    // CRM
    public DbSet<CustomerSegment> CustomerSegments { get; set; }
    public DbSet<CustomerTag> CustomerTags { get; set; }
    public DbSet<CustomerTagAssignment> CustomerTagAssignments { get; set; }
    public DbSet<CsTicket> CsTickets { get; set; }
    public DbSet<CsTicketMessage> CsTicketMessages { get; set; }
    public DbSet<CustomerJourneyEvent> CustomerJourneyEvents { get; set; }
    public DbSet<LeadScore> LeadScores { get; set; }
    public DbSet<VocEntry> VocEntries { get; set; }
    public DbSet<SalesPipeline> SalesPipelines { get; set; }

    // ERP
    public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
    public DbSet<AccountEntry> AccountEntries { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Payroll> Payrolls { get; set; }
    public DbSet<CostAnalysis> CostAnalyses { get; set; }
    public DbSet<AccountingPeriod> AccountingPeriods { get; set; }

    // SCM
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<ProcurementOrder> ProcurementOrders { get; set; }
    public DbSet<ProcurementOrderItem> ProcurementOrderItems { get; set; }
    public DbSet<SupplierEvaluation> SupplierEvaluations { get; set; }

    // Provisioning
    public DbSet<TenantApplication> TenantApplications { get; set; }

    // Migration
    public DbSet<MigrationJob> MigrationJobs { get; set; }

    // PMS (Property Management)
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<CleaningTask> CleaningTasks { get; set; }
    public DbSet<RoomAmenityLog> RoomAmenityLogs { get; set; }

    // Blockchain / Token
    public DbSet<BlockchainTransaction> BlockchainTransactions { get; set; }
    public DbSet<TokenWallet> TokenWallets { get; set; }
    public DbSet<TokenTransaction> TokenTransactions { get; set; }

    // Social Commerce
    public DbSet<MemberGrade> MemberGrades { get; set; }
    public DbSet<Gift> Gifts { get; set; }
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // Friend System + Mini-Game
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<GameRoom> GameRooms { get; set; }
    public DbSet<GamePlayer> GamePlayers { get; set; }

    // Marketplace
    public DbSet<MarketplaceConnection> MarketplaceConnections { get; set; }
    public DbSet<MarketplaceListing> MarketplaceListings { get; set; }
    public DbSet<MarketplaceOrder> MarketplaceOrders { get; set; }

    // Partner API
    public DbSet<ApiPartner> ApiPartners { get; set; }
    public DbSet<PartnerProduct> PartnerProducts { get; set; }
    public DbSet<PartnerApiLog> PartnerApiLogs { get; set; }

    // Security (AI-SOC)
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<BlockedIp> BlockedIps { get; set; }
    public DbSet<AccountLockout> AccountLockouts { get; set; }

    // Workflow Engine
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<ProcessStep> ProcessSteps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply AES-256 field encryption to sensitive PII fields
        if (_encryptionService is not null)
        {
            var converter = new EncryptedStringConverter(_encryptionService);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Phone)
                    .HasConversion(converter)
                    .HasMaxLength(512);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.Property(e => e.Phone)
                    .HasConversion(converter)
                    .HasMaxLength(512);

                entity.Property(e => e.Address1)
                    .HasConversion(converter)
                    .HasMaxLength(512);

                entity.Property(e => e.Address2)
                    .HasConversion(converter)
                    .HasMaxLength(512);
            });

            modelBuilder.Entity<DeliveryDriver>(entity =>
            {
                entity.Property(e => e.Phone)
                    .HasConversion(converter)
                    .HasMaxLength(512);
            });
        }

        // Apply global query filters to all ITenantEntity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ShopDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, [modelBuilder]);
            }
        }

        ConfigureTenant(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureProductImage(modelBuilder);
        ConfigureProductVariant(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureCart(modelBuilder);
        ConfigureCartItem(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureAddress(modelBuilder);
        ConfigureReview(modelBuilder);
        ConfigureQnA(modelBuilder);
        ConfigureWishlist(modelBuilder);
        ConfigurePage(modelBuilder);
        ConfigurePost(modelBuilder);
        ConfigureFollow(modelBuilder);
        ConfigureHashtag(modelBuilder);
        ConfigureCoupon(modelBuilder);
        ConfigureUserPoint(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureSaint(modelBuilder);
        ConfigureLiturgicalSeason(modelBuilder);
        ConfigureOrderHistory(modelBuilder);
        ConfigureCollection(modelBuilder);
        ConfigureTenantPlan(modelBuilder);
        ConfigureProductDetailSection(modelBuilder);
        ConfigureTenantUsage(modelBuilder);
        ConfigureInvoice(modelBuilder);
        ConfigureEmailCampaign(modelBuilder);
        ConfigureMesSyncHistory(modelBuilder);
        ConfigureForecastAccuracy(modelBuilder);
        ConfigureCommissionSetting(modelBuilder);
        ConfigureCommission(modelBuilder);
        ConfigureSettlement(modelBuilder);
        ConfigureCampaignVariant(modelBuilder);
        ConfigureCampaignMetric(modelBuilder);
        ConfigureProductionPlanSuggestion(modelBuilder);
        ConfigureSocialPost(modelBuilder);
        ConfigureAutoReorderRule(modelBuilder);
        ConfigurePurchaseOrder(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureDeliveryDriver(modelBuilder);
        ConfigureDeliveryZone(modelBuilder);
        ConfigureDeliveryZoneDriver(modelBuilder);
        ConfigureDeliveryOption(modelBuilder);
        ConfigureDeliveryAssignment(modelBuilder);
        ConfigureDriverLocationHistory(modelBuilder);

        // WMS
        ConfigureWarehouseZone(modelBuilder);
        ConfigureWarehouseLocation(modelBuilder);
        ConfigureInventoryMovement(modelBuilder);
        ConfigurePickingOrder(modelBuilder);
        ConfigurePickingItem(modelBuilder);
        ConfigurePackingSlip(modelBuilder);
        ConfigureBarcodeMapping(modelBuilder);

        // CRM
        ConfigureCustomerSegment(modelBuilder);
        ConfigureCustomerTag(modelBuilder);
        ConfigureCustomerTagAssignment(modelBuilder);
        ConfigureCsTicket(modelBuilder);
        ConfigureCsTicketMessage(modelBuilder);
        ConfigureCustomerJourneyEvent(modelBuilder);

        // ERP
        ConfigureChartOfAccount(modelBuilder);
        ConfigureAccountEntry(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureAttendance(modelBuilder);
        ConfigurePayroll(modelBuilder);
        ConfigureCostAnalysis(modelBuilder);

        // Provisioning
        ConfigureTenantApplication(modelBuilder);

        // Social Commerce
        ConfigureMemberGrade(modelBuilder);
        ConfigureGift(modelBuilder);
        ConfigureChatRoom(modelBuilder);
        ConfigureChatMessage(modelBuilder);
    }

    private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges()
    {
        SetTenantId();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantId()
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0) return;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }

    private static void ConfigureTenant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.CustomDomain).IsUnique().HasFilter("\"CustomDomain\" IS NOT NULL");
            entity.HasIndex(e => e.Subdomain).IsUnique().HasFilter("\"Subdomain\" IS NOT NULL");
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Username }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsRevoked });

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsFeatured);
            entity.HasIndex(e => e.SortOrder);

            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProductImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.SortOrder });

            entity.HasOne(e => e.Product)
                .WithMany(e => e.Images)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductVariant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.SortOrder });

            entity.HasOne(e => e.Product)
                .WithMany(e => e.Variants)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.OrderNumber }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ShippingAddress)
                .WithMany()
                .HasForeignKey(e => e.ShippingAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Coupon)
                .WithMany()
                .HasForeignKey(e => e.CouponId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCartItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => e.CartId);

            entity.HasOne(e => e.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.TransactionId);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureQnA(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QnA>(entity =>
        {
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reply)
                .WithOne(r => r.QnA)
                .HasForeignKey<QnAReply>(r => r.QnAId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QnAReply>(entity =>
        {
            entity.HasIndex(e => e.QnAId).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWishlist(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            entity.HasIndex(e => e.IsPublished);
        });
    }

    private static void ConfigurePost(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.PostType);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PostImage>(entity =>
        {
            entity.HasIndex(e => new { e.PostId, e.SortOrder });

            entity.HasOne(e => e.Post)
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ParentId);

            entity.HasOne(e => e.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.HasIndex(e => new { e.PostId, e.UserId, e.ReactionType }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFollow(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FollowerId, e.FollowingId }).IsUnique();
            entity.HasIndex(e => e.FollowerId);
            entity.HasIndex(e => e.FollowingId);

            entity.HasOne(e => e.Follower)
                .WithMany()
                .HasForeignKey(e => e.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Following)
                .WithMany()
                .HasForeignKey(e => e.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureHashtag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hashtag>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Tag }).IsUnique();
            entity.HasIndex(e => e.PostCount);
        });

        modelBuilder.Entity<PostHashtag>(entity =>
        {
            entity.HasIndex(e => new { e.PostId, e.HashtagId }).IsUnique();

            entity.HasOne(e => e.Post)
                .WithMany(p => p.PostHashtags)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Hashtag)
                .WithMany(h => h.PostHashtags)
                .HasForeignKey(e => e.HashtagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCoupon(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        modelBuilder.Entity<UserCoupon>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.CouponId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsUsed);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Coupon)
                .WithMany(c => c.UserCoupons)
                .HasForeignKey(e => e.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UsedOrder)
                .WithMany()
                .HasForeignKey(e => e.UsedOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureUserPoint(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPoint>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PointHistory>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TransactionType);
            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSaint(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Saint>(entity =>
        {
            entity.HasIndex(e => e.FeastDay);
            entity.HasIndex(e => e.KoreanName);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureLiturgicalSeason(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LiturgicalSeason>(entity =>
        {
            entity.HasIndex(e => new { e.Year, e.SeasonName }).IsUnique();
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });
    }

    private static void ConfigureOrderHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderHistory>(entity =>
        {
            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Histories)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCollection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsPublic);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionItem>(entity =>
        {
            entity.HasIndex(e => new { e.CollectionId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.CollectionId);

            entity.HasOne(e => e.Collection)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenantPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantPlan>(entity =>
        {
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasIndex(e => e.BillingStatus);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProductDetailSection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductDetailSection>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.SortOrder });

            entity.HasOne(e => e.Product)
                .WithMany(p => p.DetailSections)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenantUsage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantUsage>(entity =>
        {
            entity.HasIndex(e => e.TenantId).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInvoice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.BillingPeriod }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEmailCampaign(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailCampaign>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
        });
    }

    private static void ConfigureMesSyncHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MesSyncHistory>(entity =>
        {
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
        });
    }

    private static void ConfigureForecastAccuracy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForecastAccuracy>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId, e.TargetDate });
            entity.HasIndex(e => e.ForecastDate);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCommissionSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommissionSetting>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId, e.CategoryId }).IsUnique();
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCommission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Commission>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.OrderId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Settlement).WithMany(s => s.Commissions).HasForeignKey(e => e.SettlementId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureSettlement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.PeriodStart, e.PeriodEnd });
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCampaignVariant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CampaignVariant>(entity =>
        {
            entity.HasIndex(e => new { e.CampaignId, e.VariantName }).IsUnique();
            entity.HasOne(e => e.Campaign).WithMany().HasForeignKey(e => e.CampaignId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCampaignMetric(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CampaignMetric>(entity =>
        {
            entity.HasIndex(e => new { e.CampaignId, e.EventType });
            entity.HasIndex(e => new { e.VariantId, e.EventType });
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Campaign).WithMany().HasForeignKey(e => e.CampaignId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Variant).WithMany().HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureProductionPlanSuggestion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductionPlanSuggestion>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.ProductId, e.Status });
            entity.HasIndex(e => e.Urgency);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSocialPost(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SocialPost>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Platform });
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureAutoReorderRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutoReorderRule>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePurchaseOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.OrderNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TriggerType);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasOne(e => e.PurchaseOrder).WithMany(po => po.Items).HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    private static void ConfigureDeliveryDriver(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryDriver>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsApproved);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDeliveryZone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryZone>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureDeliveryZoneDriver(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryZoneDriver>(entity =>
        {
            entity.HasIndex(e => new { e.DeliveryZoneId, e.DeliveryDriverId }).IsUnique();

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.ZoneDrivers)
                .HasForeignKey(e => e.DeliveryZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.ZoneDrivers)
                .HasForeignKey(e => e.DeliveryDriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDeliveryOption(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryOption>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.DeliveryType });
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
        });
    }

    private static void ConfigureDeliveryAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryAssignment>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.DeliveryDriverId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.Status });

            entity.HasOne(e => e.Order)
                .WithOne(o => o.DeliveryAssignment)
                .HasForeignKey<DeliveryAssignment>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Driver)
                .WithMany()
                .HasForeignKey(e => e.DeliveryDriverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DeliveryOption)
                .WithMany()
                .HasForeignKey(e => e.DeliveryOptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureDriverLocationHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DriverLocationHistory>(entity =>
        {
            entity.HasIndex(e => new { e.DeliveryDriverId, e.RecordedAt });
            entity.HasIndex(e => e.DeliveryAssignmentId);

            entity.HasOne(e => e.Driver)
                .WithMany()
                .HasForeignKey(e => e.DeliveryDriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // === WMS Configurations ===

    private static void ConfigureWarehouseZone(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseZone>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        });
    }

    private static void ConfigureWarehouseLocation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.ProductId });

            entity.HasOne(e => e.Zone)
                .WithMany(z => z.Locations)
                .HasForeignKey(e => e.WarehouseZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureInventoryMovement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId });
            entity.HasIndex(e => e.MovementType);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FromLocation)
                .WithMany()
                .HasForeignKey(e => e.FromLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ToLocation)
                .WithMany()
                .HasForeignKey(e => e.ToLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePickingOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PickingOrder>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PickingNumber }).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePickingItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PickingItem>(entity =>
        {
            entity.HasOne(e => e.PickingOrder)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PickingOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePackingSlip(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackingSlip>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PackingNumber }).IsUnique();
            entity.HasIndex(e => e.TrackingNumber);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PickingOrder)
                .WithMany()
                .HasForeignKey(e => e.PickingOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PackedByUser)
                .WithMany()
                .HasForeignKey(e => e.PackedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureBarcodeMapping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BarcodeMapping>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Barcode }).IsUnique();
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }

    // === CRM Configurations ===

    private static void ConfigureCustomerSegment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerSegment>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        });
    }

    private static void ConfigureCustomerTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerTag>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        });
    }

    private static void ConfigureCustomerTagAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerTagAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CustomerTagId }).IsUnique().HasFilter("\"CustomerTagId\" IS NOT NULL");
            entity.HasIndex(e => new { e.UserId, e.CustomerSegmentId }).HasFilter("\"CustomerSegmentId\" IS NOT NULL");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.Assignments)
                .HasForeignKey(e => e.CustomerTagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Segment)
                .WithMany(s => s.TagAssignments)
                .HasForeignKey(e => e.CustomerSegmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCsTicket(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CsTicket>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.TicketNumber }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AssignedToUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureCsTicketMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CsTicketMessage>(entity =>
        {
            entity.HasOne(e => e.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(e => e.CsTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCustomerJourneyEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerJourneyEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId });
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // === ERP Configurations ===

    private static void ConfigureChartOfAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChartOfAccount>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.AccountCode }).IsUnique();
        });
    }

    private static void ConfigureAccountEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntry>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EntryNumber }).IsUnique();
            entity.HasIndex(e => e.EntryDate);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Entries)
                .HasForeignKey(e => e.ChartOfAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeNumber }).IsUnique();
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureAttendance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }).IsUnique();
            entity.HasIndex(e => e.WorkDate);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Attendances)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePayroll(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.PayPeriod }).IsUnique();
            entity.HasIndex(e => e.PayPeriod);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Payrolls)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCostAnalysis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostAnalysis>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId, e.AnalysisPeriod });
            entity.HasIndex(e => e.AnalysisPeriod);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // === Provisioning ===

    private static void ConfigureTenantApplication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantApplication>(entity =>
        {
            entity.HasIndex(e => e.DesiredSlug);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Email);

            entity.HasOne(e => e.ProvisionedTenant)
                .WithMany()
                .HasForeignKey(e => e.ProvisionedTenantId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // === Social Commerce ===

    private static void ConfigureMemberGrade(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MemberGrade>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.Grade);
            entity.HasIndex(e => e.GradePoints);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureGift(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gift>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FromUserId });
            entity.HasIndex(e => new { e.TenantId, e.ToUserId });
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.FromUser)
                .WithMany()
                .HasForeignKey(e => e.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToUser)
                .WithMany()
                .HasForeignKey(e => e.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChatRoom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.User1Id, e.User2Id });
            entity.HasIndex(e => e.LastMessageAt);

            entity.HasOne(e => e.User1)
                .WithMany()
                .HasForeignKey(e => e.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User2)
                .WithMany()
                .HasForeignKey(e => e.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureChatMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => new { e.ChatRoomId, e.CreatedAt });
            entity.HasIndex(e => new { e.SenderId, e.IsRead });

            entity.HasOne(e => e.ChatRoom)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
