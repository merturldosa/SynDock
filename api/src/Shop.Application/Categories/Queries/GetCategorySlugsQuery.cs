using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Categories.Queries;

public record CategorySlugDto(string Slug);

public record GetCategorySlugsQuery : IRequest<IReadOnlyList<CategorySlugDto>>;

public class GetCategorySlugsQueryHandler : IRequestHandler<GetCategorySlugsQuery, IReadOnlyList<CategorySlugDto>>
{
    private readonly IShopDbContext _db;

    public GetCategorySlugsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategorySlugDto>> Handle(GetCategorySlugsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategorySlugDto(c.Slug))
            .ToListAsync(cancellationToken);
    }
}
