using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record RejectDeliveryCommand(int AssignmentId, string? Reason) : IRequest<Result<bool>>;

public class RejectDeliveryCommandHandler : IRequestHandler<RejectDeliveryCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RejectDeliveryCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RejectDeliveryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var assignment = await _db.DeliveryAssignments
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result<bool>.Failure("Delivery assignment not found.");

        if (assignment.Status != nameof(DeliveryAssignmentStatus.Offered))
            return Result<bool>.Failure("Only offered assignments can be rejected.");

        // Cancel this assignment and create a new pending one for re-offer
        assignment.Status = nameof(DeliveryAssignmentStatus.Cancelled);
        assignment.CancelledAt = DateTime.UtcNow;
        assignment.CancelReason = request.Reason ?? "Driver rejected";
        assignment.UpdatedBy = _currentUser.Username ?? "system";
        assignment.UpdatedAt = DateTime.UtcNow;

        // Create new pending assignment for re-offer
        var newAssignment = new DeliveryAssignment
        {
            OrderId = assignment.OrderId,
            DeliveryOptionId = assignment.DeliveryOptionId,
            Status = nameof(DeliveryAssignmentStatus.Pending),
            CreatedBy = "system"
        };

        await _db.DeliveryAssignments.AddAsync(newAssignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
