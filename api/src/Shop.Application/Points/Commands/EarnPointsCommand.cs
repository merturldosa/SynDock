using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Points.Commands;

public record EarnPointsCommand(
    int UserId,
    decimal Amount,
    string? Description,
    int? OrderId
) : IRequest<Result<bool>>;

public class EarnPointsCommandHandler : IRequestHandler<EarnPointsCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public EarnPointsCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(EarnPointsCommand request, CancellationToken cancellationToken)
    {
        // Get or create user point balance
        var userPoint = await _db.UserPoints
            .FirstOrDefaultAsync(up => up.UserId == request.UserId, cancellationToken);

        if (userPoint is null)
        {
            userPoint = new UserPoint
            {
                UserId = request.UserId,
                Balance = 0,
                CreatedBy = "system"
            };
            await _db.UserPoints.AddAsync(userPoint, cancellationToken);
        }

        userPoint.Balance += request.Amount;

        // Create history record
        var history = new PointHistory
        {
            UserId = request.UserId,
            Amount = request.Amount,
            TransactionType = nameof(PointTransactionType.Earned),
            Description = request.Description ?? "주문 적립",
            OrderId = request.OrderId,
            CreatedBy = "system"
        };

        await _db.PointHistories.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
