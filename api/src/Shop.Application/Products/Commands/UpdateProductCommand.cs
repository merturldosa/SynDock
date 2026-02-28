using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Products.Commands;

public record UpdateProductCommand(
    int ProductId,
    string Name,
    string? Slug,
    string? Description,
    decimal Price,
    decimal? SalePrice,
    string PriceType,
    string? Specification,
    int CategoryId,
    bool IsActive,
    bool IsFeatured,
    bool IsNew,
    string? CustomFieldsJson
) : IRequest<Result<bool>>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            return Result<bool>.Failure("상품을 찾을 수 없습니다.");

        // Validate category
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
            return Result<bool>.Failure("카테고리를 찾을 수 없습니다.");

        // Check slug uniqueness if changed
        if (!string.IsNullOrEmpty(request.Slug) && request.Slug != product.Slug)
        {
            var slugExists = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.Slug == request.Slug && p.Id != request.ProductId, cancellationToken);

            if (slugExists)
                return Result<bool>.Failure($"이미 사용 중인 슬러그입니다: {request.Slug}");
        }

        product.Name = request.Name;
        product.Slug = request.Slug;
        product.Description = request.Description;
        product.Price = request.Price;
        product.SalePrice = request.SalePrice;
        product.PriceType = request.PriceType;
        product.Specification = request.Specification;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.IsFeatured = request.IsFeatured;
        product.IsNew = request.IsNew;
        product.CustomFieldsJson = request.CustomFieldsJson;
        product.UpdatedBy = _currentUser.Username;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
