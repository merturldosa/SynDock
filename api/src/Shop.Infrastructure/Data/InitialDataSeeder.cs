using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shop.Domain.Entities;
using Shop.Domain.Enums;

namespace Shop.Infrastructure.Data;

public static class InitialDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ShopDbContext>>();

        try
        {
            // PlatformAdmin 확인/생성
            await EnsurePlatformAdmin(db, logger);

            // Catholia 테넌트
            await EnsureTenant(db, logger, "catholia", "Catholia", "catholia",
                CatholiaSeedData.GetConfig(),
                CatholiaSeedData.GetCategories(),
                CatholiaSeedData.GetProducts(),
                "catholia@syndock.com", "카톨리아 관리자");

            // MoHyun 테넌트
            await EnsureTenant(db, logger, "mohyun", "모현", "mohyun",
                MoHyunSeedData.GetConfig(),
                MoHyunSeedData.GetCategories(),
                MoHyunSeedData.GetProducts(),
                "mohyun@syndock.com", "모현 관리자");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initial data seeding");
        }
    }

    private static async Task EnsurePlatformAdmin(ShopDbContext db, ILogger logger)
    {
        var exists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Role == nameof(UserRole.PlatformAdmin));

        if (exists) return;

        // Catholia 테넌트 확인 (PlatformAdmin도 어딘가에 소속 필요)
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "catholia");
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Slug = "catholia",
                Name = "Catholia",
                Subdomain = "catholia",
                IsActive = true,
                CreatedBy = "System"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var admin = new User
        {
            TenantId = tenant.Id,
            Username = "admin",
            Email = "admin@syndock.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Name = "Platform Admin",
            Role = nameof(UserRole.PlatformAdmin),
            IsActive = true,
            EmailVerified = true,
            CreatedBy = "System"
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("PlatformAdmin created: admin@syndock.com");
    }

    private static async Task EnsureTenant(
        ShopDbContext db, ILogger logger,
        string slug, string name, string subdomain,
        TenantConfig config,
        List<Application.Platform.Commands.SeedCategoryDto> categories,
        List<Application.Platform.Commands.SeedProductDto> products,
        string adminEmail, string adminName)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        var isNew = tenant is null;

        if (isNew)
        {
            tenant = new Tenant
            {
                Slug = slug,
                Name = name,
                Subdomain = subdomain,
                IsActive = true,
                ConfigJson = JsonSerializer.Serialize(config, JsonOptions),
                CreatedBy = "System"
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            logger.LogInformation("Tenant created: {Slug}", slug);
        }
        else if (string.IsNullOrEmpty(tenant!.ConfigJson))
        {
            tenant.ConfigJson = JsonSerializer.Serialize(config, JsonOptions);
            tenant.UpdatedBy = "System";
            tenant.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogInformation("Tenant config updated: {Slug}", slug);
        }

        // TenantPlan
        var hasPlan = await db.TenantPlans
            .IgnoreQueryFilters()
            .AnyAsync(tp => tp.TenantId == tenant!.Id);

        if (!hasPlan)
        {
            db.TenantPlans.Add(new TenantPlan
            {
                TenantId = tenant!.Id,
                PlanType = "Pro",
                MonthlyPrice = 0,
                BillingStatus = "Active",
                CreatedBy = "System"
            });
            await db.SaveChangesAsync();
        }

        // TenantUsage
        var hasUsage = await db.TenantUsages
            .IgnoreQueryFilters()
            .AnyAsync(tu => tu.TenantId == tenant!.Id);

        if (!hasUsage)
        {
            db.TenantUsages.Add(new TenantUsage
            {
                TenantId = tenant!.Id,
                CurrentPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                CreatedBy = "System"
            });
            await db.SaveChangesAsync();
        }

        // TenantAdmin
        var hasAdmin = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == tenant!.Id && u.Role == nameof(UserRole.TenantAdmin));

        if (!hasAdmin)
        {
            db.Users.Add(new User
            {
                TenantId = tenant!.Id,
                Username = slug + "-admin",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
                Name = adminName,
                Role = nameof(UserRole.TenantAdmin),
                IsActive = true,
                EmailVerified = true,
                CreatedBy = "System"
            });
            await db.SaveChangesAsync();
            logger.LogInformation("TenantAdmin created for {Slug}: {Email}", slug, adminEmail);
        }

        // Categories
        var categoryMap = new Dictionary<string, int>();

        foreach (var catDto in categories)
        {
            var catId = await EnsureCategory(db, tenant!.Id, catDto.Slug, catDto.Name,
                catDto.Description, catDto.Icon, catDto.SortOrder, null);
            categoryMap[catDto.Slug] = catId;

            if (catDto.Children is { Count: > 0 })
            {
                foreach (var child in catDto.Children)
                {
                    var childId = await EnsureCategory(db, tenant.Id, child.Slug, child.Name,
                        child.Description, child.Icon, child.SortOrder, catId);
                    categoryMap[child.Slug] = childId;
                }
            }
        }

        // Products
        foreach (var prodDto in products)
        {
            if (!categoryMap.TryGetValue(prodDto.CategorySlug, out var categoryId))
                continue;

            var exists = await db.Products
                .IgnoreQueryFilters()
                .AnyAsync(p => p.TenantId == tenant!.Id && p.Slug == prodDto.Slug);

            if (exists) continue;

            db.Products.Add(new Product
            {
                TenantId = tenant!.Id,
                Name = prodDto.Name,
                Slug = prodDto.Slug,
                Description = prodDto.Description,
                Price = prodDto.Price,
                SalePrice = prodDto.SalePrice,
                PriceType = "Fixed",
                CategoryId = categoryId,
                Specification = prodDto.Specification,
                IsFeatured = prodDto.IsFeatured,
                IsNew = prodDto.IsNew,
                IsActive = true,
                CreatedBy = "SeedSystem"
            });
        }

        await db.SaveChangesAsync();

        var prodCount = await db.Products.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenant!.Id);
        var catCount = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.TenantId == tenant!.Id);
        logger.LogInformation("Tenant {Slug} seeded: {CatCount} categories, {ProdCount} products",
            slug, catCount, prodCount);
    }

    private static async Task<int> EnsureCategory(
        ShopDbContext db, int tenantId, string slug, string name,
        string? description, string? icon, int sortOrder, int? parentId)
    {
        var existing = await db.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Slug == slug);

        if (existing is not null) return existing.Id;

        var category = new Category
        {
            TenantId = tenantId,
            Name = name,
            Slug = slug,
            Description = description,
            Icon = icon,
            SortOrder = sortOrder,
            ParentId = parentId,
            IsActive = true,
            CreatedBy = "SeedSystem"
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }
}
