using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record LowStockItemDto(
    int VariantId,
    int ProductId,
    string ProductName,
    string VariantName,
    string? Sku,
    int Stock,
    string? ImageUrl);

public record GetLowStockQuery(int Threshold = 10) : IRequest<Result<IReadOnlyList<LowStockItemDto>>>;

public class GetLowStockQueryHandler : IRequestHandler<GetLowStockQuery, Result<IReadOnlyList<LowStockItemDto>>>
{
    private readonly IShopDbContext _db;

    public GetLowStockQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<LowStockItemDto>>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.Stock <= request.Threshold)
            .OrderBy(v => v.Stock)
            .Select(v => new LowStockItemDto(
                v.Id,
                v.ProductId,
                v.Product.Name,
                v.Name,
                v.Sku,
                v.Stock,
                v.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LowStockItemDto>>.Success(items);
    }
}
