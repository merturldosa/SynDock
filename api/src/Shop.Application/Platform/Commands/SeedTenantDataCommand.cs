using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record SeedTenantDataCommand(
    int TenantId,
    string TemplateType,
    List<SeedCategoryDto> Categories,
    List<SeedProductDto>? Products = null,
    TenantConfig? Config = null
) : IRequest<Result<SeedTenantResultDto>>;

public record SeedCategoryDto(
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    int SortOrder,
    List<SeedCategoryDto>? Children = null);

public record SeedProductDto(
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? SalePrice,
    string CategorySlug,
    bool IsFeatured = false,
    bool IsNew = true,
    string? Specification = null,
    string? ImageUrl = null);

public record SeedTenantResultDto(
    int CategoriesCreated,
    int ProductsCreated,
    bool ConfigUpdated);

public class SeedTenantDataCommandHandler : IRequestHandler<SeedTenantDataCommand, Result<SeedTenantResultDto>>
{
    private readonly IShopDbContext _db;

    public SeedTenantDataCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SeedTenantResultDto>> Handle(SeedTenantDataCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Result<SeedTenantResultDto>.Failure("테넌트를 찾을 수 없습니다.");

        var categoriesCreated = 0;
        var productsCreated = 0;
        var categoryMap = new Dictionary<string, int>(); // slug → id

        // 1. Create categories (with children)
        foreach (var catDto in request.Categories)
        {
            var existing = await _db.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == request.TenantId && c.Slug == catDto.Slug, cancellationToken);

            if (existing is not null)
            {
                categoryMap[catDto.Slug] = existing.Id;
                continue;
            }

            var category = new Category
            {
                TenantId = request.TenantId,
                Name = catDto.Name,
                Slug = catDto.Slug,
                Description = catDto.Description,
                Icon = catDto.Icon,
                SortOrder = catDto.SortOrder,
                IsActive = true,
                CreatedBy = "SeedSystem"
            };
            await _db.Categories.AddAsync(category, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            categoryMap[catDto.Slug] = category.Id;
            categoriesCreated++;

            // Children
            if (catDto.Children is { Count: > 0 })
            {
                foreach (var childDto in catDto.Children)
                {
                    var childExisting = await _db.Categories
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.TenantId == request.TenantId && c.Slug == childDto.Slug, cancellationToken);

                    if (childExisting is not null)
                    {
                        categoryMap[childDto.Slug] = childExisting.Id;
                        continue;
                    }

                    var child = new Category
                    {
                        TenantId = request.TenantId,
                        Name = childDto.Name,
                        Slug = childDto.Slug,
                        Description = childDto.Description,
                        Icon = childDto.Icon,
                        ParentId = category.Id,
                        SortOrder = childDto.SortOrder,
                        IsActive = true,
                        CreatedBy = "SeedSystem"
                    };
                    await _db.Categories.AddAsync(child, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                    categoryMap[childDto.Slug] = child.Id;
                    categoriesCreated++;
                }
            }
        }

        // 2. Create products
        if (request.Products is { Count: > 0 })
        {
            foreach (var prodDto in request.Products)
            {
                if (!categoryMap.TryGetValue(prodDto.CategorySlug, out var categoryId))
                    continue;

                var existing = await _db.Products
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.TenantId == request.TenantId && p.Slug == prodDto.Slug, cancellationToken);

                if (existing)
                    continue;

                var product = new Product
                {
                    TenantId = request.TenantId,
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
                    SortOrder = productsCreated,
                    CreatedBy = "SeedSystem"
                };
                await _db.Products.AddAsync(product, cancellationToken);
                productsCreated++;

                // Add image if provided
                if (!string.IsNullOrEmpty(prodDto.ImageUrl))
                {
                    await _db.ProductImages.AddAsync(new ProductImage
                    {
                        TenantId = request.TenantId,
                        ProductId = product.Id,
                        Url = prodDto.ImageUrl,
                        SortOrder = 0,
                        CreatedBy = "SeedSystem"
                    }, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        // 3. Update ConfigJson if provided
        var configUpdated = false;
        if (request.Config is not null)
        {
            var onboarding = request.Config.Onboarding ?? new OnboardingConfig();
            onboarding.IsCompleted = true;
            onboarding.TemplateType = request.TemplateType;
            onboarding.CompletedAt = DateTime.UtcNow;
            request.Config.Onboarding = onboarding;

            tenant.ConfigJson = JsonSerializer.Serialize(request.Config, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            tenant.UpdatedBy = "SeedSystem";
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            configUpdated = true;
        }

        return Result<SeedTenantResultDto>.Success(
            new SeedTenantResultDto(categoriesCreated, productsCreated, configUpdated));
    }
}
