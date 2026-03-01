using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Admin.Commands;

public record UpdateStockCommand(int VariantId, int NewStock) : IRequest<Result<bool>>;

public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStockCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        if (request.NewStock < 0)
            return Result<bool>.Failure("재고 수량은 0 이상이어야 합니다.");

        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant is null)
            return Result<bool>.Failure("상품 옵션을 찾을 수 없습니다.");

        variant.Stock = request.NewStock;
        variant.UpdatedBy = _currentUser.Username;
        variant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
