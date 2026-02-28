using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Categories.Commands;

public record CreateCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    string? Icon,
    int? ParentId,
    int SortOrder
) : IRequest<Result<int>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        // Validate parent if provided
        if (request.ParentId.HasValue)
        {
            var parentExists = await _db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);

            if (!parentExists)
                return Result<int>.Failure("상위 카테고리를 찾을 수 없습니다.");
        }

        // Check slug uniqueness
        if (!string.IsNullOrEmpty(request.Slug))
        {
            var slugExists = await _db.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Slug == request.Slug, cancellationToken);

            if (slugExists)
                return Result<int>.Failure($"이미 사용 중인 슬러그입니다: {request.Slug}");
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Icon = request.Icon,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(category.Id);
    }
}
