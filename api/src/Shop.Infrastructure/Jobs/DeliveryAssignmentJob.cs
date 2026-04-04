using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;

namespace Shop.Infrastructure.Jobs;

public class DeliveryAssignmentJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IDriverLocationService _locationService;
    private readonly ILogger<DeliveryAssignmentJob> _logger;

    public DeliveryAssignmentJob(IServiceProvider services, IDriverLocationService locationService, ILogger<DeliveryAssignmentJob> logger)
    {
        _services = services;
        _locationService = locationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeliveryAssignmentJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAssignments(stoppingToken);
                await CheckExpiredOffers(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeliveryAssignmentJob");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessPendingAssignments(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
        var driverNotifier = scope.ServiceProvider.GetRequiredService<IDriverNotifier>();

        var pendingAssignments = await db.DeliveryAssignments
            .Include(a => a.Order).ThenInclude(o => o.ShippingAddress)
            .Include(a => a.DeliveryOption)
            .Where(a => a.Status == nameof(DeliveryAssignmentStatus.Pending))
            .ToListAsync(ct);

        foreach (var assignment in pendingAssignments)
        {
            try
            {
                // Find zones that cover the delivery address
                var address = assignment.Order.ShippingAddress;
                if (address is null)
                {
                    _logger.LogWarning("Order {OrderId} has no shipping address, skipping assignment", assignment.OrderId);
                    continue;
                }

                // Find online, approved drivers in matching zones
                var onlineDrivers = await db.DeliveryDrivers
                    .Where(d => d.Status == nameof(DriverStatus.Online) && d.IsApproved && d.IsActive)
                    .Include(d => d.ZoneDrivers).ThenInclude(zd => zd.Zone)
                    .ToListAsync(ct);

                if (!onlineDrivers.Any())
                    continue;

                // Find the nearest driver with location data
                int? nearestDriverId = null;
                double minDistance = double.MaxValue;

                foreach (var driver in onlineDrivers)
                {
                    var location = _locationService.GetLatestLocation(driver.Id);
                    if (location is null) continue;

                    // Check if driver is within any of their assigned zones
                    var inZone = driver.ZoneDrivers.Any(zd =>
                        zd.Zone.IsActive &&
                        _locationService.CalculateDistanceKm(
                            zd.Zone.CenterLatitude, zd.Zone.CenterLongitude,
                            location.Value.Lat, location.Value.Lng) <= zd.Zone.RadiusKm);

                    if (!inZone) continue;

                    // Check max distance for delivery option
                    if (assignment.DeliveryOption is not null && driver.LastLatitude.HasValue && driver.LastLongitude.HasValue)
                    {
                        var dist = _locationService.CalculateDistanceKm(
                            location.Value.Lat, location.Value.Lng,
                            driver.LastLatitude.Value, driver.LastLongitude.Value);

                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestDriverId = driver.Id;
                        }
                    }
                    else
                    {
                        nearestDriverId ??= driver.Id;
                    }
                }

                if (nearestDriverId.HasValue)
                {
                    assignment.DeliveryDriverId = nearestDriverId.Value;
                    assignment.Status = nameof(DeliveryAssignmentStatus.Offered);
                    assignment.OfferedAt = DateTime.UtcNow;
                    assignment.UpdatedBy = "system";
                    assignment.UpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync(ct);

                    // Notify driver
                    await driverNotifier.NotifyDriverDirectly(nearestDriverId.Value, "NewDeliveryOffer", new
                    {
                        assignmentId = assignment.Id,
                        orderNumber = assignment.Order.OrderNumber,
                        deliveryType = assignment.DeliveryOption?.DeliveryType,
                        fee = assignment.DeliveryOption?.AdditionalFee ?? 0
                    }, ct);

                    _logger.LogInformation("Offered delivery assignment {AssignmentId} to driver {DriverId}",
                        assignment.Id, nearestDriverId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing assignment {AssignmentId}", assignment.Id);
            }
        }
    }

    private async Task CheckExpiredOffers(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();

        var expiredOffers = await db.DeliveryAssignments
            .Where(a => a.Status == nameof(DeliveryAssignmentStatus.Offered)
                && a.OfferedAt.HasValue
                && a.OfferedAt.Value.AddSeconds(60) < DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var offer in expiredOffers)
        {
            offer.Status = nameof(DeliveryAssignmentStatus.Cancelled);
            offer.CancelledAt = DateTime.UtcNow;
            offer.CancelReason = "Offer expired";
            offer.UpdatedBy = "system";
            offer.UpdatedAt = DateTime.UtcNow;

            // Create new pending assignment for re-offer
            await db.DeliveryAssignments.AddAsync(new Domain.Entities.DeliveryAssignment
            {
                OrderId = offer.OrderId,
                DeliveryOptionId = offer.DeliveryOptionId,
                Status = nameof(DeliveryAssignmentStatus.Pending),
                CreatedBy = "system"
            }, ct);

            _logger.LogInformation("Offer expired for assignment {AssignmentId}, creating new pending", offer.Id);
        }

        if (expiredOffers.Any())
            await db.SaveChangesAsync(ct);
    }
}
