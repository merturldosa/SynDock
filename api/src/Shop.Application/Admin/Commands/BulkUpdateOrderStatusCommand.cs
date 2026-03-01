using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Admin.Commands;

public record BulkUpdateOrderStatusCommand(int[] OrderIds, string Status) : IRequest<Result<BulkUpdateResultDto>>;

public record BulkUpdateResultDto(int SuccessCount, int FailCount, string[] Errors);

public class BulkUpdateOrderStatusCommandHandler : IRequestHandler<BulkUpdateOrderStatusCommand, Result<BulkUpdateResultDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public BulkUpdateOrderStatusCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BulkUpdateResultDto>> Handle(BulkUpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, out _))
            return Result<BulkUpdateResultDto>.Failure($"유효하지 않은 주문 상태입니다: {request.Status}");

        var orders = await _db.Orders
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var successCount = 0;
        var errors = new List<string>();

        foreach (var order in orders)
        {
            if (!IsValidTransition(order.Status, request.Status))
            {
                errors.Add($"{order.OrderNumber}: '{order.Status}'에서 '{request.Status}'로 변경 불가");
                continue;
            }

            order.Status = request.Status;
            order.UpdatedBy = _currentUser.Username;
            order.UpdatedAt = DateTime.UtcNow;

            var history = new OrderHistory
            {
                OrderId = order.Id,
                Status = request.Status,
                Note = $"일괄 상태 변경: {request.Status}",
                CreatedBy = _currentUser.Username ?? "system"
            };
            await _db.OrderHistories.AddAsync(history, cancellationToken);

            var notification = new Notification
            {
                UserId = order.UserId,
                Type = nameof(NotificationType.Order),
                Title = GetNotificationTitle(request.Status),
                Message = $"주문 상태가 변경되었습니다.",
                ReferenceId = order.Id,
                ReferenceType = "Order",
                CreatedBy = "system"
            };
            await _db.Notifications.AddAsync(notification, cancellationToken);

            successCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BulkUpdateResultDto>.Success(new BulkUpdateResultDto(successCount, errors.Count, errors.ToArray()));
    }

    private static bool IsValidTransition(string current, string next)
    {
        return (current, next) switch
        {
            (nameof(OrderStatus.Pending), nameof(OrderStatus.Confirmed)) => true,
            (nameof(OrderStatus.Pending), nameof(OrderStatus.Cancelled)) => true,
            (nameof(OrderStatus.Confirmed), nameof(OrderStatus.Processing)) => true,
            (nameof(OrderStatus.Confirmed), nameof(OrderStatus.Cancelled)) => true,
            (nameof(OrderStatus.Processing), nameof(OrderStatus.Shipped)) => true,
            (nameof(OrderStatus.Shipped), nameof(OrderStatus.Delivered)) => true,
            (nameof(OrderStatus.Delivered), nameof(OrderStatus.Refunded)) => true,
            _ => false
        };
    }

    private static string GetNotificationTitle(string status) => status switch
    {
        nameof(OrderStatus.Confirmed) => "주문이 확인되었습니다",
        nameof(OrderStatus.Processing) => "상품 준비가 시작되었습니다",
        nameof(OrderStatus.Shipped) => "상품이 발송되었습니다",
        nameof(OrderStatus.Delivered) => "배송이 완료되었습니다",
        nameof(OrderStatus.Cancelled) => "주문이 취소되었습니다",
        nameof(OrderStatus.Refunded) => "환불이 처리되었습니다",
        _ => "주문 상태가 변경되었습니다"
    };
}
