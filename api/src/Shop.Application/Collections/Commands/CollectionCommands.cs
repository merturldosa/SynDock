using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Collections.Commands;

// ── Create Collection ──
public record CreateCollectionCommand(string Name, string? Description, bool IsPublic) : IRequest<Result<int>>;

public class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCollectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var collection = new Collection
        {
            UserId = _currentUser.UserId.Value,
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            CreatedBy = _currentUser.Username ?? "user"
        };

        await _db.Collections.AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(collection.Id);
    }
}

// ── Add to Collection ──
public record AddToCollectionCommand(int CollectionId, int ProductId, string? Note) : IRequest<Result<bool>>;

public class AddToCollectionCommandHandler : IRequestHandler<AddToCollectionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AddToCollectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(AddToCollectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var collection = await _db.Collections
            .FirstOrDefaultAsync(c => c.Id == request.CollectionId && c.UserId == _currentUser.UserId.Value, cancellationToken);

        if (collection is null)
            return Result<bool>.Failure("Collection not found.");

        var exists = await _db.CollectionItems
            .AnyAsync(ci => ci.CollectionId == request.CollectionId && ci.ProductId == request.ProductId, cancellationToken);

        if (exists)
            return Result<bool>.Failure("Product is already in the collection.");

        var item = new CollectionItem
        {
            CollectionId = request.CollectionId,
            ProductId = request.ProductId,
            Note = request.Note,
            AddedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Username ?? "user"
        };

        await _db.CollectionItems.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

// ── Remove from Collection ──
public record RemoveFromCollectionCommand(int CollectionId, int ProductId) : IRequest<Result<bool>>;

public class RemoveFromCollectionCommandHandler : IRequestHandler<RemoveFromCollectionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromCollectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RemoveFromCollectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var item = await _db.CollectionItems
            .Include(ci => ci.Collection)
            .FirstOrDefaultAsync(ci => ci.CollectionId == request.CollectionId
                && ci.ProductId == request.ProductId
                && ci.Collection.UserId == _currentUser.UserId.Value, cancellationToken);

        if (item is null)
            return Result<bool>.Failure("Item not found.");

        _db.CollectionItems.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

// ── Delete Collection ──
public record DeleteCollectionCommand(int CollectionId) : IRequest<Result<bool>>;

public class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCollectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var collection = await _db.Collections
            .FirstOrDefaultAsync(c => c.Id == request.CollectionId && c.UserId == _currentUser.UserId.Value, cancellationToken);

        if (collection is null)
            return Result<bool>.Failure("Collection not found.");

        _db.Collections.Remove(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
