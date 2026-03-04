using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record UpdateProfileCommand(string? Name, string? Phone) : IRequest<Result<UserProfileDto>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<UserProfileDto>.Failure("로그인이 필요합니다.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
            return Result<UserProfileDto>.Failure("사용자를 찾을 수 없습니다.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name;

        if (request.Phone is not null)
            user.Phone = request.Phone;

        user.UpdatedBy = _currentUser.Username;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<UserProfileDto>.Success(new UserProfileDto(
            user.Id, user.Username, user.Email, user.Name, user.Phone,
            user.Role, user.CustomFieldsJson, user.LastLoginAt, user.EmailVerified));
    }
}
