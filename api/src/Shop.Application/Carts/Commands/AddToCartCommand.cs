using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Carts.Commands;

public record AddToCartCommand(
    int ProductId,
    int? VariantId,
    int Quantity = 1
) : IRequest<Result<int>>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AddToCartCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        // Validate product exists and is active
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (product is null)
            return Result<int>.Failure("Product not found.");

        // Validate variant if specified + stock check
        if (request.VariantId.HasValue)
        {
            var variant = await _db.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == request.VariantId.Value && v.ProductId == request.ProductId && v.IsActive, cancellationToken);

            if (variant is null)
                return Result<int>.Failure("Product variant not found.");

            if (variant.Stock < request.Quantity)
                return Result<int>.Failure($"재고가 부족합니다. (현재: {variant.Stock}개, 요청: {request.Quantity}개)");
        }
        else
        {
            // Check default variant stock
            var defaultVariant = await _db.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.ProductId == request.ProductId && v.IsActive, cancellationToken);

            if (defaultVariant != null && defaultVariant.Stock < request.Quantity)
                return Result<int>.Failure($"재고가 부족합니다. (현재: {defaultVariant.Stock}개, 요청: {request.Quantity}개)");
        }

        // Get or create cart
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedBy = _currentUser.Username ?? "system"
            };
            await _db.Carts.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Check if same product+variant already in cart
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.VariantId == request.VariantId);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UpdatedBy = _currentUser.Username;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                CreatedBy = _currentUser.Username ?? "system"
            };
            await _db.CartItems.AddAsync(cartItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(cart.Id);
    }
}
