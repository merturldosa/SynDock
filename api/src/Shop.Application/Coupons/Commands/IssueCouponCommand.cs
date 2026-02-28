using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Commands;

public record IssueCouponCommand(
    int CouponId,
    List<int>? UserIds
) : IRequest<Result<int>>;

public class IssueCouponCommandHandler : IRequestHandler<IssueCouponCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public IssueCouponCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(IssueCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _db.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.CouponId, cancellationToken);

        if (coupon is null)
            return Result<int>.Failure("쿠폰을 찾을 수 없습니다.");

        // Determine target users
        List<int> targetUserIds;
        if (request.UserIds is { Count: > 0 })
        {
            targetUserIds = request.UserIds;
        }
        else
        {
            // Issue to all active users
            targetUserIds = await _db.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        // Filter out users who already have this coupon
        var existingUserIds = await _db.UserCoupons
            .Where(uc => uc.CouponId == request.CouponId)
            .Select(uc => uc.UserId)
            .ToListAsync(cancellationToken);

        var newUserIds = targetUserIds.Except(existingUserIds).ToList();

        var userCoupons = newUserIds.Select(userId => new UserCoupon
        {
            UserId = userId,
            CouponId = request.CouponId,
            IsUsed = false,
            CreatedBy = _currentUser.Username ?? "system"
        }).ToList();

        await _db.UserCoupons.AddRangeAsync(userCoupons, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(userCoupons.Count);
    }
}
