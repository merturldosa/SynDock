using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Queries;

public record ValidateCouponQuery(string Code, decimal OrderAmount) : IRequest<Result<CouponValidationResult>>;

public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, Result<CouponValidationResult>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ValidateCouponQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CouponValidationResult>> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<CouponValidationResult>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;

        var coupon = await _db.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == request.Code.ToUpper().Trim(), cancellationToken);

        if (coupon is null)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "존재하지 않는 쿠폰 코드입니다.", 0));

        if (!coupon.IsActive)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "비활성화된 쿠폰입니다.", 0));

        if (now < coupon.StartDate)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "아직 사용 기간이 아닙니다.", 0));

        if (now > coupon.EndDate)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "만료된 쿠폰입니다.", 0));

        if (coupon.MaxUsageCount > 0 && coupon.CurrentUsageCount >= coupon.MaxUsageCount)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "쿠폰 사용 한도를 초과했습니다.", 0));

        if (request.OrderAmount < coupon.MinOrderAmount)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, $"최소 주문 금액 {coupon.MinOrderAmount:N0}원 이상이어야 합니다.", 0));

        // Check if user has this coupon and hasn't used it
        var userCoupon = await _db.UserCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id, cancellationToken);

        if (userCoupon is null)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "발급받지 않은 쿠폰입니다.", 0));

        if (userCoupon.IsUsed)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "이미 사용한 쿠폰입니다.", 0));

        // Calculate discount
        decimal discountAmount;
        if (coupon.DiscountType == nameof(CouponType.Percentage))
        {
            discountAmount = request.OrderAmount * coupon.DiscountValue / 100;
            if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                discountAmount = coupon.MaxDiscountAmount.Value;
        }
        else
        {
            discountAmount = coupon.DiscountValue;
        }

        if (discountAmount > request.OrderAmount)
            discountAmount = request.OrderAmount;

        return Result<CouponValidationResult>.Success(new CouponValidationResult(true, null, discountAmount));
    }
}
