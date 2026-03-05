using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Points.Commands;

public record UsePointsCommand(
    int UserId,
    decimal Amount,
    int OrderId
) : IRequest<Result<bool>>;

public class UsePointsCommandHandler : IRequestHandler<UsePointsCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public UsePointsCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UsePointsCommand request, CancellationToken cancellationToken)
    {
        var userPoint = await _db.UserPoints
            .FirstOrDefaultAsync(up => up.UserId == request.UserId, cancellationToken);

        if (userPoint is null || userPoint.Balance < request.Amount)
            return Result<bool>.Failure("Insufficient points.");

        userPoint.Balance -= request.Amount;

        var history = new PointHistory
        {
            UserId = request.UserId,
            Amount = -request.Amount,
            TransactionType = nameof(PointTransactionType.Used),
            Description = "주문 사용",
            OrderId = request.OrderId,
            CreatedBy = "system"
        };

        await _db.PointHistories.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
