using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.ProductDetailSections.Queries;

public record GetProductDetailSectionsQuery(int ProductId) : IRequest<Result<List<ProductDetailSectionDto>>>;

public class GetProductDetailSectionsQueryHandler : IRequestHandler<GetProductDetailSectionsQuery, Result<List<ProductDetailSectionDto>>>
{
    private readonly IShopDbContext _db;

    public GetProductDetailSectionsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ProductDetailSectionDto>>> Handle(GetProductDetailSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _db.ProductDetailSections
            .AsNoTracking()
            .Where(s => s.ProductId == request.ProductId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ProductDetailSectionDto(
                s.Id, s.Title, s.Content, s.ImageUrl,
                s.ImageAltText, s.SectionType, s.SortOrder, s.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<ProductDetailSectionDto>>.Success(sections);
    }
}
