using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Categories.Queries;

public record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryTreeDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryTreeDto>>
{
    private readonly IShopDbContext _db;

    public GetCategoriesQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Include(c => c.Products.Where(p => p.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        var roots = categories.Where(c => c.ParentId == null).ToList();

        return roots.Select(r => BuildTree(r, categories)).ToList();
    }

    private static CategoryTreeDto BuildTree(Domain.Entities.Category category, List<Domain.Entities.Category> allCategories)
    {
        var children = allCategories
            .Where(c => c.ParentId == category.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => BuildTree(c, allCategories))
            .ToList();

        var productCount = category.Products?.Count ?? 0;

        return new CategoryTreeDto(
            category.Id, category.Name, category.Slug, category.Icon,
            category.SortOrder, productCount, children);
    }
}
