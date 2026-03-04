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
}
