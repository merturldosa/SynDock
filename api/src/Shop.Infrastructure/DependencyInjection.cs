using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Payments;
using Shop.Infrastructure.Repositories;
using Shop.Infrastructure.Services;
using Shop.Infrastructure.Shipping;
using Shop.Infrastructure.Jobs;
using Shop.Application.Liturgy.Services;
using Shop.Infrastructure.AI;
using Shop.Infrastructure.Integration;
using Shop.Infrastructure.Storage;
using Shop.Infrastructure.Services.Scm;
using Shop.Infrastructure.Blockchain;
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<ShopDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ShopDb"));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IShopDbContext>(sp => sp.GetRequiredService<ShopDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ITotpService, TotpService>();

        // AES-256 field encryption
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // GeoIP (IP → Country detection for i18n)
        services.AddSingleton<IGeoIpService, GeoIpService>();

        // Transfer service (정산금 이체 - Mock → 실제 은행 API로 교체 가능)
        services.AddScoped<ITransferService, MockTransferService>();

        // TenantContext is registered as Scoped (one per request)
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // Payment providers
        services.AddScoped<MockPaymentProvider>();
        services.AddHttpClient<TossPaymentProvider>();
        services.AddScoped<TenantAwarePaymentProvider>();
        services.AddScoped<IPaymentProvider>(sp => sp.GetRequiredService<TenantAwarePaymentProvider>());

        services.AddSingleton<ILiturgicalCalendarService, LiturgicalCalendarService>();

        // Shipping tracker
        services.AddHttpClient<IShippingTracker, DeliveryTrackerService>();
        services.AddHostedService<ShippingStatusUpdateService>();

        // File storage
        var uploadsPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5100";
        services.AddSingleton<LocalFileStorageService>(new LocalFileStorageService(uploadsPath, baseUrl));

        var s3Endpoint = configuration["Storage:S3:Endpoint"];
        if (!string.IsNullOrEmpty(s3Endpoint))
        {
            // S3-compatible CDN (Cloudflare R2 / AWS S3 / MinIO) with local fallback
            services.AddHttpClient("S3Storage");
            services.AddSingleton<IFileStorageService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("S3Storage");
                var config = sp.GetRequiredService<IConfiguration>();
                var localFallback = sp.GetRequiredService<LocalFileStorageService>();
                var logger = sp.GetRequiredService<ILogger<CdnFileStorageService>>();
                return new CdnFileStorageService(httpClient, config, localFallback, logger);
            });
        }
        else
        {
            // Local file storage only
            services.AddSingleton<IFileStorageService>(sp =>
                sp.GetRequiredService<LocalFileStorageService>());
        }

        services.AddHttpContextAccessor();

        // OAuth
        services.AddHttpClient<IOAuthProviderService, OAuthProviderService>();

        // Email
        services.AddScoped<IEmailService, SmtpEmailService>();

        // PDF
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddSingleton<IPdfService, PdfGenerationService>();

        // Image Processing
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();

        // AI
        services.AddHttpClient<IAIChatProvider, ClaudeAIChatProvider>();
        services.AddHttpClient<IImageGenerator, DalleImageGenerator>();
        services.AddScoped<IRecommendationEngine, CollaborativeFilteringEngine>();
        services.AddScoped<IAiForecastInsightService, ClaudeAiForecastInsightService>();
        services.AddHttpClient<IAiHomepageGenerator, AiHomepageGenerator>();
        services.AddScoped<IDemandForecastService, DemandForecastService>();

        // Production Plan
        services.AddScoped<IProductionPlanService, ProductionPlanService>();

        // MES Integration
        services.AddScoped<IMesProductMapper, MesProductMapper>();
        var mesMode = configuration["Mes:Enabled"]?.ToLower();
        if (mesMode == "true")
        {
            services.AddHttpClient<IMesClient, MesHttpClient>();
        }
        else if (mesMode == "demo")
        {
            services.AddSingleton<IMesClient, DemoMesClient>();
        }
        else
        {
            services.AddSingleton<IMesClient, NullMesClient>();
        }

        // Social Media (Instagram/Facebook auto-posting)
        services.AddHttpClient<ISocialMediaService, SocialMediaService>();

        // Kakao Alimtalk
        services.AddHttpClient<IKakaoAlimtalkService, KakaoAlimtalkService>(client =>
        {
            client.BaseAddress = new Uri(configuration["KakaoAlimtalk:BaseUrl"] ?? "https://kapi.kakao.com");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Web Push
        services.AddHttpClient("WebPush");
        services.AddScoped<IWebPushService, WebPushService>();

        // Currency
        services.AddSingleton<ICurrencyService, CurrencyService>();

        // Plan enforcement
        services.AddScoped<IPlanEnforcer, PlanEnforcer>();

        // Delivery
        services.AddSingleton<IDriverLocationService, InMemoryDriverLocationService>();

        // Background jobs
        services.AddHostedService<DeliveryAssignmentJob>();
        services.AddHostedService<BillingScheduler>();
        services.AddHostedService<TrialExpiryJob>();
        services.AddHostedService<MesInventorySyncJob>();
        services.AddHostedService<ForecastAccuracyUpdateJob>();
        services.AddHostedService<SettlementScheduler>();
        services.AddHostedService<CampaignScheduler>();
        services.AddHostedService<AutoReorderJob>();
        services.AddHostedService<BirthdayCouponJob>();
        services.AddHostedService<CartAbandonmentJob>();
        services.AddHostedService<RepurchaseReminderJob>();
        services.AddHostedService<LotExpirationAlertJob>();
        services.AddHostedService<SupplierPerformanceJob>();
        services.AddHostedService<LeadScoreRecalculationJob>();
        services.AddHostedService<AiSupplyChainOrchestrator>();
        services.AddHostedService<AiBusinessAutomator>();
        services.AddHostedService<MemberGradeRecalculationJob>();
        services.AddHostedService<MarketplaceStockSyncJob>();
        services.AddHostedService<SecurityCleanupJob>();

        // Security (AI-SOC)
        services.AddScoped<ISecurityMonitorService, SecurityMonitorService>();

        // Workflow Engine
        services.AddScoped<IWorkflowService, WorkflowService>();

        // Auto Coupon
        services.AddScoped<IAutoCouponService, AutoCouponService>();

        // WMS
        services.AddScoped<IWmsService, WmsService>();

        // PMS (Property Management)
        services.AddScoped<IPmsService, PmsService>();

        // Blockchain / Token
        services.AddScoped<IBlockchainService, BlockchainService>();

        // CRM
        services.AddScoped<ICrmService, CrmService>();

        // ERP
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IHrService, HrService>();

        // SCM
        services.AddScoped<IScmService, ScmService>();

        // Social Commerce
        services.AddScoped<ISocialCommerceService, SocialCommerceService>();

        // Friend System + Mini-Game
        services.AddScoped<IFriendGameService, FriendGameService>();

        // Marketplace
        services.AddScoped<IMarketplaceService, MarketplaceService>();

        // Provisioning & Subscription
        services.AddScoped<IProvisioningService, ProvisioningService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Shop Migration (crawler/importer)
        services.AddHttpClient<IShopMigrationService, ShopMigrationService>();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "Shop:";
            });
        }

        return services;
    }
}
