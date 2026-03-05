using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Queries;

public record GetShippingTrackingQuery(int OrderId) : IRequest<Result<ShippingTrackingResult>>;

public class GetShippingTrackingQueryHandler : IRequestHandler<GetShippingTrackingQuery, Result<ShippingTrackingResult>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IShippingTracker _shippingTracker;

    public GetShippingTrackingQueryHandler(
        IShopDbContext db,
        ICurrentUserService currentUser,
        IShippingTracker shippingTracker)
    {
        _db = db;
        _currentUser = currentUser;
        _shippingTracker = shippingTracker;
    }

    public async Task<Result<ShippingTrackingResult>> Handle(GetShippingTrackingQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<ShippingTrackingResult>.Failure("Authentication required.");

        // Get the latest shipping history with tracking info
        var history = await _db.OrderHistories
            .Where(h => h.OrderId == request.OrderId && h.TrackingNumber != null)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (history is null || string.IsNullOrEmpty(history.TrackingNumber))
            return Result<ShippingTrackingResult>.Failure("No tracking information available.");

        var carrier = history.TrackingCarrier ?? "CJ대한통운";
        var result = await _shippingTracker.GetTrackingInfo(carrier, history.TrackingNumber, cancellationToken);

        return Result<ShippingTrackingResult>.Success(result);
    }
}
