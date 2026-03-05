using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Categories.Commands;

public record UpdateCategoryCommand(
    int CategoryId,
    string Name,
    string? Slug,
    string? Description,
    string? Icon,
    int? ParentId,
    int SortOrder,
    bool IsActive
) : IRequest<Result<bool>>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
            return Result<bool>.Failure("Category not found.");

        // Prevent circular parent reference
        if (request.ParentId == request.CategoryId)
            return Result<bool>.Failure("Cannot set a category as its own parent.");

        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);

            if (!parentExists)
                return Result<bool>.Failure("Parent category not found.");
        }

        // Check slug uniqueness if changed
        if (!string.IsNullOrEmpty(request.Slug) && request.Slug != category.Slug)
        {
            var slugExists = await _db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Slug == request.Slug && c.Id != request.CategoryId, cancellationToken);

            if (slugExists)
                return Result<bool>.Failure($"Slug already in use: {request.Slug}");
        }

        category.Name = request.Name;
        category.Slug = request.Slug;
        category.Description = request.Description;
        category.Icon = request.Icon;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedBy = _currentUser.Username;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
