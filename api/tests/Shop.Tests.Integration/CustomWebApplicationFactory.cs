using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtSecret = "SynDockShopTestOnlyKey2026!@#$%^&*()_+DevEnvironment";
    private const string TestJwtIssuer = "SynDock.Shop";
    private const string TestJwtAudience = "SynDock.Shop.Client";

    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString("N");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateLogger();

        builder.UseSerilog();

        var host = base.CreateHost(builder);

        // Seed after host is built so we use the actual ServiceProvider
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Encryption:Key"] = "RGV2T25seUFFUzI1NktleUZvclRlc3RpbmchMTIzNDU=",
                ["Mes:WebhookSecret"] = "test-webhook-secret",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ShopDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // Remove hosted services (background jobs) to avoid interference
            var hostedServices = services.Where(
                d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var svc in hostedServices) services.Remove(svc);

            // Remove Redis cache
            var cacheDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);

            // Add InMemory DB (use stable name per factory instance)
            services.AddDbContext<ShopDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Add InMemory distributed cache (replaces Redis)
            services.AddDistributedMemoryCache();
        });
    }

    private static void SeedTestData(ShopDbContext db)
    {
        if (db.Tenants.Any()) return;

        db.Tenants.Add(new Tenant { Id = 1, Slug = "catholia", Name = "Catholia", IsActive = true, Subdomain = "catholia" });
        db.Tenants.Add(new Tenant { Id = 2, Slug = "mohyun", Name = "MoHyun", IsActive = true, Subdomain = "mohyun" });
        db.Tenants.Add(new Tenant { Id = 3, Slug = "inactive", Name = "Inactive", IsActive = false, Subdomain = "inactive" });

        db.Users.Add(new User
        {
            Id = 1, TenantId = 1, Username = "admin", Email = "admin@catholia.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Name = "Admin", Role = "Admin", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 2, TenantId = 1, Username = "member", Email = "member@catholia.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"),
            Name = "Member", Role = "Member", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 3, TenantId = 2, Username = "mohyun_admin", Email = "admin@mohyun.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Name = "MoHyun Admin", Role = "Admin", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 4, TenantId = 1, Username = "tenantadmin", Email = "tenantadmin@catholia.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TenantAdmin123!"),
            Name = "Tenant Admin", Role = "TenantAdmin", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 5, TenantId = 0, Username = "platformadmin", Email = "platform@syndock.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Platform123!"),
            Name = "Platform Admin", Role = "PlatformAdmin", IsActive = true
        });

        db.Categories.Add(new Category { Id = 1, TenantId = 1, Name = "성물", Slug = "sacred-items", IsActive = true });
        db.Categories.Add(new Category { Id = 2, TenantId = 2, Name = "장류", Slug = "fermented-sauce", IsActive = true });

        db.Products.Add(new Product { Id = 1, TenantId = 1, Name = "묵주", Slug = "rosary", Price = 15000m, CategoryId = 1, IsActive = true });
        db.Products.Add(new Product { Id = 2, TenantId = 2, Name = "된장", Slug = "doenjang", Price = 12000m, CategoryId = 2, IsActive = true });

        db.ProductVariants.Add(new ProductVariant { Id = 1, TenantId = 1, ProductId = 1, Name = "Default", Stock = 100, Price = 0, IsActive = true });
        db.ProductVariants.Add(new ProductVariant { Id = 2, TenantId = 2, ProductId = 2, Name = "Default", Stock = 50, Price = 0, IsActive = true });

        db.SaveChanges();
    }

    public static string GenerateTestToken(
        int userId = 1, string username = "admin", string email = "admin@catholia.com",
        string role = "Admin", int tenantId = 1, string tenantSlug = "catholia",
        int expirationMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_slug", tenantSlug),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer, audience: TestJwtAudience,
            claims: claims, expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClient(
        int userId = 1, string username = "admin", string role = "Admin",
        int tenantId = 1, string tenantSlug = "catholia")
    {
        var client = CreateClient();
        var token = GenerateTestToken(userId, username, role: role, tenantId: tenantId, tenantSlug: tenantSlug);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantSlug);
        return client;
    }
}
