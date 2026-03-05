using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Collections.Queries;

// ── DTOs ──
public record CollectionDto(int Id, string Name, string? Description, bool IsPublic, int ItemCount, DateTime CreatedAt);

public record CollectionItemDto(int ProductId, string ProductName, decimal Price, decimal? SalePrice, string? ImageUrl, string? Note, DateTime AddedAt);

public record CollectionDetailDto(int Id, string Name, string? Description, bool IsPublic, List<CollectionItemDto> Items);

// ── Get My Collections ──
public record GetMyCollectionsQuery : IRequest<Result<List<CollectionDto>>>;

public class GetMyCollectionsQueryHandler : IRequestHandler<GetMyCollectionsQuery, Result<List<CollectionDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCollectionsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<CollectionDto>>> Handle(GetMyCollectionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<List<CollectionDto>>.Failure("Authentication required.");

        var collections = await _db.Collections
            .AsNoTracking()
            .Where(c => c.UserId == _currentUser.UserId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CollectionDto(
                c.Id,
                c.Name,
                c.Description,
                c.IsPublic,
                c.Items.Count,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<CollectionDto>>.Success(collections);
    }
}

// ── Get Collection Detail ──
public record GetCollectionDetailQuery(int CollectionId) : IRequest<Result<CollectionDetailDto>>;

public class GetCollectionDetailQueryHandler : IRequestHandler<GetCollectionDetailQuery, Result<CollectionDetailDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCollectionDetailQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CollectionDetailDto>> Handle(GetCollectionDetailQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<CollectionDetailDto>.Failure("Authentication required.");

        var collection = await _db.Collections
            .AsNoTracking()
            .Where(c => c.Id == request.CollectionId && c.UserId == _currentUser.UserId.Value)
            .Select(c => new CollectionDetailDto(
                c.Id,
                c.Name,
                c.Description,
                c.IsPublic,
                c.Items.OrderByDescending(i => i.AddedAt).Select(i => new CollectionItemDto(
                    i.ProductId,
                    i.Product.Name,
                    i.Product.Price,
                    i.Product.SalePrice,
                    i.Product.Images.Where(img => img.IsPrimary).Select(img => img.Url).FirstOrDefault(),
                    i.Note,
                    i.AddedAt
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null)
            return Result<CollectionDetailDto>.Failure("Collection not found.");

        return Result<CollectionDetailDto>.Success(collection);
    }
}
