using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Banners.Queries;

public record GetActiveBannersQuery(string? PageTarget = null) : IRequest<List<BannerDto>>;

public record GetAllBannersQuery(int Page = 1, int PageSize = 20) : IRequest<BannerListDto>;

public record BannerDto(int Id, string Title, string? Description, string? ImageUrl, string? LinkUrl,
    string DisplayType, string? PageTarget, DateTime? StartDate, DateTime? EndDate, int SortOrder, bool IsActive);

public record BannerListDto(List<BannerDto> Items, int TotalCount);

public class GetActiveBannersQueryHandler : IRequestHandler<GetActiveBannersQuery, List<BannerDto>>
{
    private readonly IShopDbContext _db;

    public GetActiveBannersQueryHandler(IShopDbContext db) => _db = db;

    public async Task<List<BannerDto>> Handle(GetActiveBannersQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _db.Banners.Where(b => b.IsActive
            && (b.StartDate == null || b.StartDate <= now)
            && (b.EndDate == null || b.EndDate >= now));

        if (!string.IsNullOrEmpty(request.PageTarget))
            query = query.Where(b => b.PageTarget == null || b.PageTarget == request.PageTarget);

        return await query.OrderBy(b => b.SortOrder)
            .Select(b => new BannerDto(b.Id, b.Title, b.Description, b.ImageUrl, b.LinkUrl,
                b.DisplayType, b.PageTarget, b.StartDate, b.EndDate, b.SortOrder, b.IsActive))
            .ToListAsync(cancellationToken);
    }
}

public class GetAllBannersQueryHandler : IRequestHandler<GetAllBannersQuery, BannerListDto>
{
    private readonly IShopDbContext _db;

    public GetAllBannersQueryHandler(IShopDbContext db) => _db = db;

    public async Task<BannerListDto> Handle(GetAllBannersQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await _db.Banners.CountAsync(cancellationToken);
        var items = await _db.Banners
            .OrderBy(b => b.SortOrder)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BannerDto(b.Id, b.Title, b.Description, b.ImageUrl, b.LinkUrl,
                b.DisplayType, b.PageTarget, b.StartDate, b.EndDate, b.SortOrder, b.IsActive))
            .ToListAsync(cancellationToken);

        return new BannerListDto(items, totalCount);
    }
}
