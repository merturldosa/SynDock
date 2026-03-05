using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using Shop.API;
using Shop.API.Hubs;
using Shop.API.Middleware;
using Shop.API.Resources;
using Shop.Application;
using Shop.Application.Common.Interfaces;
using Shop.Infrastructure;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Logging;

// Serilog bootstrap
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SynDock.Shop API...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    var logPath = Path.Combine(builder.Environment.ContentRootPath, "Logs", "log-.txt");
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .Enrich.FromLogContext()
        .Enrich.With<PiiEnricher>()
        .Destructure.With<PiiDestructurePolicy>());

    // Sentry error monitoring
    var sentryDsn = builder.Configuration["Sentry:Dsn"];
    if (!string.IsNullOrEmpty(sentryDsn))
    {
        builder.WebHost.UseSentry(o =>
        {
            o.Dsn = sentryDsn;
            o.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.2;
            o.Environment = builder.Environment.EnvironmentName;
        });
    }

    // Application & Infrastructure DI
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]!;
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // SignalR sends JWT via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // General: 100 requests per minute per IP
        options.AddFixedWindowLimiter("general", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        // Auth: 10 requests per minute per IP (login/register)
        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // SignalR
    builder.Services.AddSignalR();
    builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();
    builder.Services.AddScoped<IAdminDashboardNotifier, SignalRAdminDashboardNotifier>();

    // Localization (i18n) with GeoIP auto-detection
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[]
        {
            new CultureInfo("ko"),      // 한국어
            new CultureInfo("en"),      // English
            new CultureInfo("ja"),      // 日本語
            new CultureInfo("zh-CN"),   // 中文
            new CultureInfo("vi"),      // Tiếng Việt
        };
        options.DefaultRequestCulture = new RequestCulture("ko");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
        // Priority: QueryString (?culture=) > Accept-Language header > GeoIP (IP-based)
        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider(),
            new GeoIpCultureProvider()
        };
    });

    // Health Checks
    var healthBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("ShopDb")!,
            name: "postgresql",
            tags: ["db", "ready"])
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
            tags: ["live"]);

    var redisConn = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConn))
    {
        healthBuilder.AddRedis(redisConn, name: "redis", tags: ["cache", "ready"]);
    }

    // Controllers
    builder.Services.AddControllers();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SynDock.Shop API",
            Version = "v1",
            Description = "SynDock.Shop - Multi-tenant Shopping Mall Platform API"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
        // Add X-Tenant-Id header to Swagger
        c.OperationFilter<TenantHeaderFilter>();
    });

    // CORS — origins from config
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3200", "http://localhost:3100"];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Auto-create database in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        // Seed initial tenant data (Catholia + MoHyun)
        await InitialDataSeeder.SeedAsync(app.Services);
    }

    // HTTP pipeline
    app.UseHttpMetrics(); // Prometheus request metrics
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SynDock.Shop API v1"));
    }
    else
    {
        app.UseHttpsRedirection();
        app.UseHsts();
    }

    app.UseRequestLocalization();
    app.UseStaticFiles();
    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseMiddleware<ResponseCachingMiddleware>();

    // Tenant resolution middleware
    app.UseMiddleware<TenantMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    // PII masking: masks other users' email/phone in API responses
    app.UseMiddleware<PiiMaskingMiddleware>();

    app.MapControllers();
    app.MapHub<NotificationHub>("/api/hubs/notifications");
    app.MapHub<AdminHub>("/api/hubs/admin");

    // Health check endpoints
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = WriteHealthCheckResponse
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse
    });
    // Prometheus metrics endpoint (nginx blocks external access; internal only)
    app.MapMetrics("/metrics");

    app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = WriteHealthCheckResponse
    });

    static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            platform = "SynDock.Shop",
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    Log.Information("SynDock.Shop API started successfully on port 5100");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program class accessible for WebApplicationFactory
public partial class Program { }
