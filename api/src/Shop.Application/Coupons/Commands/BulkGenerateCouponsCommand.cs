using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Coupons.Commands;

public record BulkGenerateCouponsCommand(
    string Prefix,
    int Count,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsageCount
) : IRequest<Result<List<string>>>;

public class BulkGenerateCouponsCommandHandler : IRequestHandler<BulkGenerateCouponsCommand, Result<List<string>>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public BulkGenerateCouponsCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<string>>> Handle(BulkGenerateCouponsCommand request, CancellationToken cancellationToken)
    {
        if (request.Count < 1 || request.Count > 10000)
            return Result<List<string>>.Failure("Count must be between 1 and 10000.");

        var codes = new List<string>();
        var prefix = request.Prefix.ToUpper().Trim();

        for (var i = 0; i < request.Count; i++)
        {
            var code = $"{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            codes.Add(code);

            var coupon = new Coupon
            {
                Code = code,
                Name = request.Name,
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MinOrderAmount = request.MinOrderAmount,
                MaxDiscountAmount = request.MaxDiscountAmount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MaxUsageCount = request.MaxUsageCount,
                IsActive = true,
                CreatedBy = _currentUser.Username ?? "system"
            };

            await _db.Coupons.AddAsync(coupon, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<List<string>>.Success(codes);
    }
}
