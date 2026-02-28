using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record CreateNotificationCommand(
    int UserId,
    string Type,
    string Title,
    string? Message,
    int? ReferenceId,
    string? ReferenceType
) : IRequest<Result<int>>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public CreateNotificationCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            IsRead = false,
            CreatedBy = "system"
        };

        await _db.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(notification.Id);
    }
}
