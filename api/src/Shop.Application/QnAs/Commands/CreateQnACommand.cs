using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.QnAs.Commands;

public record CreateQnACommand(
    int ProductId,
    string Title,
    string Content,
    bool IsSecret
) : IRequest<Result<int>>;

public class CreateQnACommandHandler : IRequestHandler<CreateQnACommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQnACommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateQnACommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var productExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
            return Result<int>.Failure("Product not found.");

        var qna = new QnA
        {
            ProductId = request.ProductId,
            UserId = _currentUser.UserId.Value,
            Title = request.Title,
            Content = request.Content,
            IsSecret = request.IsSecret,
            IsAnswered = false,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.QnAs.AddAsync(qna, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(qna.Id);
    }
}
