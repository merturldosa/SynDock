using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.QnAs.Queries;

public record GetProductQnAsQuery(int ProductId, int Page = 1, int PageSize = 10) : IRequest<PagedQnAResult>;

public record PagedQnAResult(
    IReadOnlyList<QnADto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class GetProductQnAsQueryHandler : IRequestHandler<GetProductQnAsQuery, PagedQnAResult>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetProductQnAsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedQnAResult> Handle(GetProductQnAsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.QnAs
            .AsNoTracking()
            .Include(q => q.User)
            .Include(q => q.Reply!)
                .ThenInclude(r => r.User)
            .Where(q => q.ProductId == request.ProductId);

        var totalCount = await query.CountAsync(cancellationToken);

        var qnas = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var currentUserId = _currentUser.UserId;

        var items = qnas.Select(q =>
        {
            // If secret and not the owner, mask content
            var canView = !q.IsSecret || q.UserId == currentUserId;
            return new QnADto(
                q.Id, q.ProductId, q.UserId, q.User.Name,
                canView ? q.Title : "비밀글입니다.",
                canView ? q.Content : "",
                q.IsAnswered, q.IsSecret,
                q.Reply is not null && canView
                    ? new QnAReplyDto(q.Reply.Id, q.Reply.UserId, q.Reply.User.Name, q.Reply.Content, q.Reply.CreatedAt)
                    : null,
                q.CreatedAt);
        }).ToList();

        return new PagedQnAResult(items, totalCount, request.Page, request.PageSize);
    }
}
