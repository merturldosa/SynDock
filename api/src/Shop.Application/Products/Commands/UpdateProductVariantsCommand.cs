using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Products.Commands;

public record VariantDto(int? Id, string Name, string? Sku, decimal? Price, int Stock, int SortOrder, bool IsActive);

public record UpdateProductVariantsCommand(int ProductId, List<VariantDto> Variants) : IRequest<Result<bool>>;

public class UpdateProductVariantsCommandHandler : IRequestHandler<UpdateProductVariantsCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantsCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateProductVariantsCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            return Result<bool>.Failure("상품을 찾을 수 없습니다.");

        var existing = await _db.ProductVariants
            .Where(v => v.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        var incomingIds = request.Variants
            .Where(v => v.Id.HasValue)
            .Select(v => v.Id!.Value)
            .ToHashSet();

        // Delete orphans
        var toDelete = existing.Where(e => !incomingIds.Contains(e.Id)).ToList();
        foreach (var v in toDelete)
        {
            _db.ProductVariants.Remove(v);
        }

        foreach (var dto in request.Variants)
        {
            if (dto.Id.HasValue)
            {
                // Update existing
                var entity = existing.FirstOrDefault(e => e.Id == dto.Id.Value);
                if (entity is not null)
                {
                    entity.Name = dto.Name;
                    entity.Sku = dto.Sku;
                    entity.Price = dto.Price;
                    entity.Stock = dto.Stock;
                    entity.SortOrder = dto.SortOrder;
                    entity.IsActive = dto.IsActive;
                    entity.UpdatedBy = _currentUser.Username;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Create new
                var entity = new ProductVariant
                {
                    ProductId = request.ProductId,
                    TenantId = product.TenantId,
                    Name = dto.Name,
                    Sku = dto.Sku,
                    Price = dto.Price,
                    Stock = dto.Stock,
                    SortOrder = dto.SortOrder,
                    IsActive = dto.IsActive,
                    CreatedBy = _currentUser.Username ?? "system"
                };
                await _db.ProductVariants.AddAsync(entity, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
