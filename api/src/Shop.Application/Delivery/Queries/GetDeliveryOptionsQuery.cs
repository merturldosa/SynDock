using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Delivery.Queries;

public record DeliveryOptionDto(
    int Id,
    string DeliveryType,
    string DisplayName,
    string? Description,
    decimal AdditionalFee,
    int MaxDeliveryMinutes,
    double MaxDistanceKm,
    string? AvailableFrom,
    string? AvailableTo
);

public record GetDeliveryOptionsQuery : IRequest<Result<List<DeliveryOptionDto>>>;

public class GetDeliveryOptionsQueryHandler : IRequestHandler<GetDeliveryOptionsQuery, Result<List<DeliveryOptionDto>>>
{
    private readonly IShopDbContext _db;

    public GetDeliveryOptionsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<DeliveryOptionDto>>> Handle(GetDeliveryOptionsQuery request, CancellationToken cancellationToken)
    {
        var options = await _db.DeliveryOptions
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .Select(o => new DeliveryOptionDto(
                o.Id,
                o.DeliveryType,
                o.DisplayName,
                o.Description,
                o.AdditionalFee,
                o.MaxDeliveryMinutes,
                o.MaxDistanceKm,
                o.AvailableFrom,
                o.AvailableTo
            ))
            .ToListAsync(cancellationToken);

        return Result<List<DeliveryOptionDto>>.Success(options);
    }
}
