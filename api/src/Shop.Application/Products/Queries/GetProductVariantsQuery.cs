using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Products.Queries;

public record ProductVariantListDto(
    int Id,
    string Name,
    string? Sku,
    decimal? Price,
    int Stock,
    int SortOrder,
    bool IsActive);

public record GetProductVariantsQuery(int ProductId) : IRequest<IReadOnlyList<ProductVariantListDto>>;

public class GetProductVariantsQueryHandler : IRequestHandler<GetProductVariantsQuery, IReadOnlyList<ProductVariantListDto>>
{
    private readonly IShopDbContext _db;

    public GetProductVariantsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductVariantListDto>> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
    {
        return await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == request.ProductId)
            .OrderBy(v => v.SortOrder)
            .Select(v => new ProductVariantListDto(
                v.Id, v.Name, v.Sku, v.Price, v.Stock, v.SortOrder, v.IsActive))
            .ToListAsync(cancellationToken);
    }
}
