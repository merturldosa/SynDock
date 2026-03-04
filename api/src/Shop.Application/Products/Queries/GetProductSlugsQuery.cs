using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Products.Queries;

public record ProductSlugDto(string Slug, string? UpdatedAt);

public record GetProductSlugsQuery : IRequest<IReadOnlyList<ProductSlugDto>>;

public class GetProductSlugsQueryHandler : IRequestHandler<GetProductSlugsQuery, IReadOnlyList<ProductSlugDto>>
{
    private readonly IShopDbContext _db;

    public GetProductSlugsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductSlugDto>> Handle(GetProductSlugsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new ProductSlugDto(p.Slug, p.UpdatedAt.HasValue ? p.UpdatedAt.Value.ToString("o") : null))
            .ToListAsync(cancellationToken);
    }
}
