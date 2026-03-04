using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ShopDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ShopDb")));

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
        services.AddSingleton<IFileStorageService>(new LocalFileStorageService(uploadsPath, baseUrl));

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
        services.AddScoped<IDemandForecastService, DemandForecastService>();

        // Production Plan
        services.AddScoped<IProductionPlanService, ProductionPlanService>();

        // MES Integration
        services.AddScoped<IMesProductMapper, MesProductMapper>();
        if (configuration.GetValue<bool>("Mes:Enabled"))
        {
            services.AddHttpClient<IMesClient, MesHttpClient>();
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

        // Background jobs
        services.AddHostedService<BillingScheduler>();
        services.AddHostedService<TrialExpiryJob>();
        services.AddHostedService<MesInventorySyncJob>();
        services.AddHostedService<ForecastAccuracyUpdateJob>();
        services.AddHostedService<SettlementScheduler>();
        services.AddHostedService<CampaignScheduler>();
        services.AddHostedService<AutoReorderJob>();
        services.AddHostedService<BirthdayCouponJob>();

        // Auto Coupon
        services.AddScoped<IAutoCouponService, AutoCouponService>();

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
