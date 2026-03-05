using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Commands;

public record CreateOrderResult(int OrderId, string OrderNumber);

public record CreateOrderCommand(
    int? ShippingAddressId,
    string? Note,
    string? CouponCode,
    decimal PointsToUse = 0
) : IRequest<Result<CreateOrderResult>>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<CreateOrderResult>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminDashboardNotifier _adminNotifier;
    private readonly IPlanEnforcer _planEnforcer;
    private readonly IEmailService _emailService;

    public CreateOrderCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IAdminDashboardNotifier adminNotifier, IPlanEnforcer planEnforcer, IEmailService emailService)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _adminNotifier = adminNotifier;
        _planEnforcer = planEnforcer;
        _emailService = emailService;
    }

    public async Task<Result<CreateOrderResult>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<CreateOrderResult>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        // Plan limit check
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        var limitCheck = await _planEnforcer.CanPlaceOrder(user.TenantId, cancellationToken);
        if (!limitCheck.IsSuccess)
            return Result<CreateOrderResult>.Failure(limitCheck.Error!);

        // Get cart with items
        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null || !cart.Items.Any())
            return Result<CreateOrderResult>.Failure("Cart is empty.");

        // Validate shipping address if provided
        if (request.ShippingAddressId.HasValue)
        {
            var address = await _db.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId.Value && a.UserId == userId, cancellationToken);

            if (address is null)
                return Result<CreateOrderResult>.Failure("Shipping address not found.");
        }

        // Generate order number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Stock validation and deduction
        foreach (var ci in cart.Items)
        {
            if (ci.VariantId.HasValue && ci.Variant is not null)
            {
                if (ci.Variant.Stock < ci.Quantity)
                    return Result<CreateOrderResult>.Failure($"'{ci.Product.Name} ({ci.Variant.Name})' 재고가 부족합니다. (현재: {ci.Variant.Stock}, 요청: {ci.Quantity})");

                ci.Variant.Stock -= ci.Quantity;
            }
        }

        // Create order items
        var orderItems = cart.Items.Select(ci =>
        {
            var unitPrice = ci.Variant?.Price ?? ci.Product.SalePrice ?? ci.Product.Price;
            return new OrderItem
            {
                ProductId = ci.ProductId,
                VariantId = ci.VariantId,
                ProductName = ci.Product.Name,
                Quantity = ci.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * ci.Quantity,
                CreatedBy = _currentUser.Username ?? "system"
            };
        }).ToList();

        var subtotal = orderItems.Sum(oi => oi.TotalPrice);
        var discountAmount = 0m;
        int? couponId = null;

        // Apply coupon
        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == request.CouponCode.ToUpper() && c.IsActive, cancellationToken);

            if (coupon is not null && coupon.StartDate <= DateTime.UtcNow && coupon.EndDate >= DateTime.UtcNow)
            {
                var userCoupon = await _db.UserCoupons
                    .FirstOrDefaultAsync(uc => uc.CouponId == coupon.Id && uc.UserId == userId && !uc.IsUsed, cancellationToken);

                if (userCoupon is not null)
                {
                    if (subtotal >= coupon.MinOrderAmount)
                    {
                        discountAmount = coupon.DiscountType == nameof(Domain.Enums.CouponType.Percentage)
                            ? subtotal * coupon.DiscountValue / 100
                            : coupon.DiscountValue;

                        if (coupon.MaxDiscountAmount is > 0 && discountAmount > coupon.MaxDiscountAmount.Value)
                            discountAmount = coupon.MaxDiscountAmount.Value;

                        couponId = coupon.Id;
                        userCoupon.IsUsed = true;
                        userCoupon.UsedAt = DateTime.UtcNow;
                        coupon.CurrentUsageCount++;
                    }
                }
            }
        }

        // Apply points
        var pointsUsed = 0m;
        if (request.PointsToUse > 0)
        {
            var userPoint = await _db.UserPoints
                .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

            if (userPoint is not null && userPoint.Balance >= request.PointsToUse)
            {
                pointsUsed = Math.Min(request.PointsToUse, subtotal - discountAmount);
                userPoint.Balance -= pointsUsed;

                await _db.PointHistories.AddAsync(new PointHistory
                {
                    UserId = userId,
                    Amount = -pointsUsed,
                    TransactionType = nameof(Domain.Enums.PointTransactionType.Used),
                    Description = $"주문 사용 ({orderNumber})",
                    CreatedBy = _currentUser.Username ?? "system"
                }, cancellationToken);
            }
        }

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Status = nameof(OrderStatus.Pending),
            TotalAmount = subtotal - discountAmount - pointsUsed,
            ShippingFee = 0,
            DiscountAmount = discountAmount,
            PointsUsed = pointsUsed,
            CouponId = couponId,
            Note = request.Note,
            ShippingAddressId = request.ShippingAddressId,
            Items = orderItems,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Orders.AddAsync(order, cancellationToken);

        // Set coupon's UsedOrderId
        if (couponId.HasValue)
        {
            var userCoupon = await _db.UserCoupons
                .FirstOrDefaultAsync(uc => uc.CouponId == couponId.Value && uc.UserId == userId, cancellationToken);
            if (userCoupon is not null)
                userCoupon.UsedOrderId = order.Id;
        }

        // Clear cart after order creation
        _db.CartItems.RemoveRange(cart.Items);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Track usage
        await _planEnforcer.IncrementOrderCount(user.TenantId, 1, cancellationToken);

        // Notify admin dashboard
        try { await _adminNotifier.NotifyNewOrder(order.TenantId, order.OrderNumber, order.TotalAmount, cancellationToken); }
        catch { /* notification failure should not block order creation */ }

        // Send order confirmation email
        try
        {
            if (!string.IsNullOrEmpty(user.Email))
            {
                var itemsHtml = string.Join("", orderItems.Select(oi =>
                    $"<tr><td style=\"padding:8px;border-bottom:1px solid #eee\">{oi.ProductName}</td>" +
                    $"<td style=\"padding:8px;border-bottom:1px solid #eee;text-align:center\">{oi.Quantity}</td>" +
                    $"<td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{oi.TotalPrice:N0}원</td></tr>"));

                var emailBody = $@"
<div style=""font-family:sans-serif;max-width:600px;margin:0 auto"">
  <h2 style=""color:#333"">주문이 접수되었습니다</h2>
  <p>주문번호: <strong>{order.OrderNumber}</strong></p>
  <table style=""width:100%;border-collapse:collapse;margin:16px 0"">
    <tr style=""background:#f5f5f5"">
      <th style=""padding:8px;text-align:left"">상품명</th>
      <th style=""padding:8px;text-align:center"">수량</th>
      <th style=""padding:8px;text-align:right"">금액</th>
    </tr>
    {itemsHtml}
  </table>
  <p style=""font-size:18px;font-weight:bold;text-align:right"">총 결제금액: {order.TotalAmount:N0}원</p>
  <p style=""color:#666;font-size:14px"">결제가 완료되면 주문이 확인됩니다. 감사합니다.</p>
</div>";
                await _emailService.SendAsync(user.Email, $"주문 접수 확인 [{order.OrderNumber}]", emailBody, cancellationToken);
            }
        }
        catch { /* Email failure should not block order creation */ }

        return Result<CreateOrderResult>.Success(new CreateOrderResult(order.Id, order.OrderNumber));
    }
}
