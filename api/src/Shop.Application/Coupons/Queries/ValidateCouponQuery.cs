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
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "Coupon code not found.", 0));

        if (!coupon.IsActive)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "This coupon is inactive.", 0));

        if (now < coupon.StartDate)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "This coupon is not yet available.", 0));

        if (now > coupon.EndDate)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "This coupon has expired.", 0));

        if (coupon.MaxUsageCount > 0 && coupon.CurrentUsageCount >= coupon.MaxUsageCount)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "Coupon usage limit exceeded.", 0));

        if (request.OrderAmount < coupon.MinOrderAmount)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, $"Minimum order amount of {coupon.MinOrderAmount:N0} required.", 0));

        // Check if user has this coupon and hasn't used it
        var userCoupon = await _db.UserCoupons
            .AsNoTracking()
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id, cancellationToken);

        if (userCoupon is null)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "Coupon not issued to this user.", 0));

        if (userCoupon.IsUsed)
            return Result<CouponValidationResult>.Success(new CouponValidationResult(false, "This coupon has already been used.", 0));

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
