using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Coupons.Queries;

public record GetCouponsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedCoupons>>;

public class GetCouponsQueryHandler : IRequestHandler<GetCouponsQuery, Result<PagedCoupons>>
{
    private readonly IShopDbContext _db;

    public GetCouponsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedCoupons>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CouponDto(
                c.Id,
                c.Code,
                c.Name,
                c.Description,
                c.DiscountType,
                c.DiscountValue,
                c.MinOrderAmount,
                c.MaxDiscountAmount,
                c.StartDate,
                c.EndDate,
                c.MaxUsageCount,
                c.CurrentUsageCount,
                c.IsActive,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedCoupons>.Success(new PagedCoupons(items, totalCount));
    }
}
