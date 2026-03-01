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
using Shop.Application.Liturgy.Services;
using Shop.Infrastructure.AI;
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

        // AI
        services.AddHttpClient<IAIChatProvider, ClaudeAIChatProvider>();
        services.AddScoped<IRecommendationEngine, SimpleRecommendationEngine>();

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
