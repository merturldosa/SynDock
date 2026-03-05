using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Banners.Commands;

public record CreateBannerCommand(
    string Title, string? Description, string? ImageUrl, string? LinkUrl,
    string DisplayType, string? PageTarget,
    DateTime? StartDate, DateTime? EndDate, int SortOrder
) : IRequest<Result<int>>;

public record UpdateBannerCommand(
    int Id, string Title, string? Description, string? ImageUrl, string? LinkUrl,
    string DisplayType, string? PageTarget,
    DateTime? StartDate, DateTime? EndDate, int SortOrder, bool IsActive
) : IRequest<Result<bool>>;

public record DeleteBannerCommand(int Id) : IRequest<Result<bool>>;

public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBannerCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = new Banner
        {
            Title = request.Title,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            DisplayType = request.DisplayType,
            PageTarget = request.PageTarget,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Banners.AddAsync(banner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(banner.Id);
    }
}

public class UpdateBannerCommandHandler : IRequestHandler<UpdateBannerCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBannerCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (banner == null)
            return Result<bool>.Failure("Banner not found.");

        banner.Title = request.Title;
        banner.Description = request.Description;
        banner.ImageUrl = request.ImageUrl;
        banner.LinkUrl = request.LinkUrl;
        banner.DisplayType = request.DisplayType;
        banner.PageTarget = request.PageTarget;
        banner.StartDate = request.StartDate;
        banner.EndDate = request.EndDate;
        banner.SortOrder = request.SortOrder;
        banner.IsActive = request.IsActive;
        banner.UpdatedBy = _currentUser.Username;
        banner.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class DeleteBannerCommandHandler : IRequestHandler<DeleteBannerCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBannerCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await _db.Banners.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
        if (banner == null)
            return Result<bool>.Failure("Banner not found.");

        _db.Banners.Remove(banner);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
