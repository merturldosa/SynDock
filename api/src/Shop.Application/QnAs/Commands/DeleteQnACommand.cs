using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.QnAs.Commands;

public record DeleteQnACommand(int QnAId) : IRequest<Result<bool>>;

public class DeleteQnACommandHandler : IRequestHandler<DeleteQnACommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQnACommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteQnACommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var qna = await _db.QnAs
            .FirstOrDefaultAsync(q => q.Id == request.QnAId && q.UserId == _currentUser.UserId.Value, cancellationToken);

        if (qna is null)
            return Result<bool>.Failure("질문을 찾을 수 없습니다.");

        _db.QnAs.Remove(qna);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
