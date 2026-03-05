using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.QnAs.Queries;

public record MyQnADto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    string Title,
    string Content,
    bool IsSecret,
    bool IsAnswered,
    DateTime CreatedAt);

public record MyQnAsResult(
    IReadOnlyList<MyQnADto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record GetMyQnAsQuery(int Page = 1, int PageSize = 10) : IRequest<Result<MyQnAsResult>>;

public class GetMyQnAsQueryHandler : IRequestHandler<GetMyQnAsQuery, Result<MyQnAsResult>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyQnAsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MyQnAsResult>> Handle(GetMyQnAsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<MyQnAsResult>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        var query = _db.QnAs
            .AsNoTracking()
            .Include(q => q.Product)
                .ThenInclude(p => p.Images)
            .Where(q => q.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new MyQnADto(
                q.Id,
                q.ProductId,
                q.Product.Name,
                q.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                q.Title,
                q.Content,
                q.IsSecret,
                q.IsAnswered,
                q.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<MyQnAsResult>.Success(
            new MyQnAsResult(items, totalCount, request.Page, request.PageSize));
    }
}
