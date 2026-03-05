using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.QnAs.Commands;

public record AnswerQnACommand(
    int QnAId,
    string Content
) : IRequest<Result<int>>;

public class AnswerQnACommandHandler : IRequestHandler<AnswerQnACommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AnswerQnACommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(AnswerQnACommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var qna = await _db.QnAs
            .FirstOrDefaultAsync(q => q.Id == request.QnAId, cancellationToken);

        if (qna is null)
            return Result<int>.Failure("Question not found.");

        if (qna.IsAnswered)
            return Result<int>.Failure("This question has already been answered.");

        var reply = new QnAReply
        {
            QnAId = request.QnAId,
            UserId = _currentUser.UserId.Value,
            Content = request.Content,
            CreatedBy = _currentUser.Username ?? "system"
        };

        qna.IsAnswered = true;
        qna.UpdatedBy = _currentUser.Username;
        qna.UpdatedAt = DateTime.UtcNow;

        await _db.QnAReplies.AddAsync(reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(reply.Id);
    }
}
