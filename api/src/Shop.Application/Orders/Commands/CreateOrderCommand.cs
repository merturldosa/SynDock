using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Commands;

public record CreateOrderCommand(
    int? ShippingAddressId,
    string? Note,
    string? CouponCode,
    decimal PointsToUse = 0
) : IRequest<Result<int>>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        // Get cart with items
        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null || !cart.Items.Any())
            return Result<int>.Failure("장바구니가 비어있습니다.");

        // Validate shipping address if provided
        if (request.ShippingAddressId.HasValue)
        {
            var address = await _db.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId.Value && a.UserId == userId, cancellationToken);

            if (address is null)
                return Result<int>.Failure("배송지를 찾을 수 없습니다.");
        }

        // Generate order number
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

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
        return Result<int>.Success(order.Id);
    }
}
