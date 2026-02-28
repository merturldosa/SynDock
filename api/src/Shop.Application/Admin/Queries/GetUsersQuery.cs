using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record UserListDto(
    int Id,
    string Username,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record GetUsersQuery : IRequest<Result<IReadOnlyList<UserListDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<IReadOnlyList<UserListDto>>>
{
    private readonly IShopDbContext _db;

    public GetUsersQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<UserListDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto(
                u.Id, u.Username, u.Name, u.Email,
                u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserListDto>>.Success(users);
    }
}
