using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Saints.Queries;

public record GetProductsBySaintQuery(int SaintId) : IRequest<List<SaintProductDto>>;

public record SaintProductDto(int ProductId, string Name, string? Slug, decimal Price, string? ImageUrl);

public class GetProductsBySaintQueryHandler : IRequestHandler<GetProductsBySaintQuery, List<SaintProductDto>>
{
    private readonly IShopDbContext _db;

    public GetProductsBySaintQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<List<SaintProductDto>> Handle(GetProductsBySaintQuery request, CancellationToken cancellationToken)
    {
        return await _db.SaintProducts
            .Where(sp => sp.SaintId == request.SaintId)
            .Include(sp => sp.Product).ThenInclude(p => p.Images)
            .Select(sp => new SaintProductDto(
                sp.ProductId,
                sp.Product.Name,
                sp.Product.Slug,
                sp.Product.Price,
                sp.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}
