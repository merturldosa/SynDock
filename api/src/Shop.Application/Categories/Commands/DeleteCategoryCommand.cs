using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Categories.Commands;

public record DeleteCategoryCommand(int CategoryId) : IRequest<Result<bool>>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var category = await _db.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
            return Result<bool>.Failure("Category not found.");

        if (category.Children.Any(c => c.IsActive))
            return Result<bool>.Failure("Cannot delete: category has active subcategories.");

        if (category.Products.Any(p => p.IsActive))
            return Result<bool>.Failure("Cannot delete: category has active products.");

        // Soft delete
        category.IsActive = false;
        category.UpdatedBy = _currentUser.Username;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
