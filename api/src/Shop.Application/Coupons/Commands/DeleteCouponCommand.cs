using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Commands;

public record DeleteCouponCommand(int Id) : IRequest<Result<bool>>;

public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCouponCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _db.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (coupon is null)
            return Result<bool>.Failure("Coupon not found.");

        _db.Coupons.Remove(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
