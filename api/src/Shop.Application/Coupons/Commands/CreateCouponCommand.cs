using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Commands;

public record CreateCouponCommand(
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsageCount
) : IRequest<Result<int>>;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCouponCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = new Coupon
        {
            Code = request.Code.ToUpper().Trim(),
            Name = request.Name,
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxUsageCount = request.MaxUsageCount,
            IsActive = true,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Coupons.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(coupon.Id);
    }
}
