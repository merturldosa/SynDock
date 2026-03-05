using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Commands;

public record UpdateCouponCommand(
    int Id,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsageCount,
    bool IsActive
) : IRequest<Result<bool>>;

public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCouponCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _db.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (coupon is null)
            return Result<bool>.Failure("Coupon not found.");

        coupon.Name = request.Name;
        coupon.Description = request.Description;
        coupon.DiscountType = request.DiscountType;
        coupon.DiscountValue = request.DiscountValue;
        coupon.MinOrderAmount = request.MinOrderAmount;
        coupon.MaxDiscountAmount = request.MaxDiscountAmount;
        coupon.StartDate = request.StartDate;
        coupon.EndDate = request.EndDate;
        coupon.MaxUsageCount = request.MaxUsageCount;
        coupon.IsActive = request.IsActive;
        coupon.UpdatedBy = _currentUser.Username;
        coupon.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
