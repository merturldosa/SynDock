using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.ProductDetailSections.Commands;

// ── Create ──
public record CreateProductDetailSectionCommand(
    int ProductId, string Title, string? Content, string? ImageUrl,
    string? ImageAltText, string SectionType, int SortOrder
) : IRequest<Result<int>>;

public class CreateProductDetailSectionCommandHandler : IRequestHandler<CreateProductDetailSectionCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductDetailSectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateProductDetailSectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        var productExists = await _db.Products.AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
            return Result<int>.Failure("상품을 찾을 수 없습니다.");

        var section = new ProductDetailSection
        {
            ProductId = request.ProductId,
            Title = request.Title,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            ImageAltText = request.ImageAltText,
            SectionType = request.SectionType,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.ProductDetailSections.AddAsync(section, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(section.Id);
    }
}

// ── Update ──
public record UpdateProductDetailSectionCommand(
    int Id, string? Title, string? Content, string? ImageUrl,
    string? ImageAltText, string? SectionType, int? SortOrder, bool? IsActive
) : IRequest<Result<bool>>;

public class UpdateProductDetailSectionCommandHandler : IRequestHandler<UpdateProductDetailSectionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductDetailSectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateProductDetailSectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var section = await _db.ProductDetailSections
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (section is null)
            return Result<bool>.Failure("섹션을 찾을 수 없습니다.");

        if (request.Title is not null) section.Title = request.Title;
        if (request.Content is not null) section.Content = request.Content;
        if (request.ImageUrl is not null) section.ImageUrl = request.ImageUrl;
        if (request.ImageAltText is not null) section.ImageAltText = request.ImageAltText;
        if (request.SectionType is not null) section.SectionType = request.SectionType;
        if (request.SortOrder.HasValue) section.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) section.IsActive = request.IsActive.Value;

        section.UpdatedBy = _currentUser.Username;
        section.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

// ── Delete ──
public record DeleteProductDetailSectionCommand(int Id) : IRequest<Result<bool>>;

public class DeleteProductDetailSectionCommandHandler : IRequestHandler<DeleteProductDetailSectionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductDetailSectionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteProductDetailSectionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var section = await _db.ProductDetailSections
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (section is null)
            return Result<bool>.Failure("섹션을 찾을 수 없습니다.");

        _db.ProductDetailSections.Remove(section);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

// ── Reorder ──
public record ReorderProductDetailSectionsCommand(int ProductId, List<int> SectionIds) : IRequest<Result<bool>>;

public class ReorderProductDetailSectionsCommandHandler : IRequestHandler<ReorderProductDetailSectionsCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderProductDetailSectionsCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ReorderProductDetailSectionsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var sections = await _db.ProductDetailSections
            .Where(s => s.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < request.SectionIds.Count; i++)
        {
            var section = sections.FirstOrDefault(s => s.Id == request.SectionIds[i]);
            if (section is not null)
                section.SortOrder = i;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
