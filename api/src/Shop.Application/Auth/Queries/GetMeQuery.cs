using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Auth.Queries;

public record GetMeQuery(int UserId) : IRequest<Result<UserProfileDto>>;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<UserProfileDto>>
{
    private readonly IShopDbContext _db;

    public GetMeQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserProfileDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result<UserProfileDto>.Failure("사용자를 찾을 수 없습니다.");

        return Result<UserProfileDto>.Success(new UserProfileDto(
            user.Id, user.Username, user.Email, user.Name, user.Phone,
            user.Role, user.CustomFieldsJson, user.LastLoginAt));
    }
}
