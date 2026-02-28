using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Queries;

public record GetMyCouponsQuery : IRequest<Result<IReadOnlyList<UserCouponDto>>>;

public class GetMyCouponsQueryHandler : IRequestHandler<GetMyCouponsQuery, Result<IReadOnlyList<UserCouponDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCouponsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserCouponDto>>> Handle(GetMyCouponsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<IReadOnlyList<UserCouponDto>>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;

        var items = await _db.UserCoupons
            .AsNoTracking()
            .Include(uc => uc.Coupon)
            .Where(uc => uc.UserId == userId && !uc.IsUsed && uc.Coupon.IsActive && uc.Coupon.EndDate > now)
            .OrderBy(uc => uc.Coupon.EndDate)
            .Select(uc => new UserCouponDto(
                uc.Id,
                uc.CouponId,
                uc.Coupon.Code,
                uc.Coupon.Name,
                uc.Coupon.Description,
                uc.Coupon.DiscountType,
                uc.Coupon.DiscountValue,
                uc.Coupon.MinOrderAmount,
                uc.Coupon.MaxDiscountAmount,
                uc.Coupon.EndDate,
                uc.IsUsed,
                uc.UsedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserCouponDto>>.Success(items);
    }
}
