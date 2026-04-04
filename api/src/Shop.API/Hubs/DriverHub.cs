using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;

namespace Shop.API.Hubs;

[Authorize]
public class DriverHub : Hub
{
    private readonly IShopDbContext _db;
    private readonly IDriverLocationService _locationService;

    public DriverHub(IShopDbContext db, IDriverLocationService locationService)
    {
        _db = db;
        _locationService = locationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task GoOnline()
    {
        var userId = GetUserId();
        if (userId is null) return;

        var driver = await _db.DeliveryDrivers
            .Include(d => d.ZoneDrivers)
            .FirstOrDefaultAsync(d => d.UserId == userId.Value);

        if (driver is null || !driver.IsApproved) return;

        driver.Status = nameof(DriverStatus.Online);
        driver.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Join driver-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"driver-{driver.Id}");

        // Join zone groups
        foreach (var zd in driver.ZoneDrivers)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"zone-{driver.TenantId}-{zd.DeliveryZoneId}");
        }
    }

    public async Task GoOffline()
    {
        var userId = GetUserId();
        if (userId is null) return;

        var driver = await _db.DeliveryDrivers
            .Include(d => d.ZoneDrivers)
            .FirstOrDefaultAsync(d => d.UserId == userId.Value);

        if (driver is null) return;

        driver.Status = nameof(DriverStatus.Offline);
        driver.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Leave zone groups
        foreach (var zd in driver.ZoneDrivers)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"zone-{driver.TenantId}-{zd.DeliveryZoneId}");
        }
    }

    public async Task UpdateLocation(double latitude, double longitude)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var driver = await _db.DeliveryDrivers
            .FirstOrDefaultAsync(d => d.UserId == userId.Value);

        if (driver is null) return;

        // Update in-memory location
        await _locationService.UpdateLocation(driver.Id, latitude, longitude);

        // Update DB last known location
        driver.LastLatitude = latitude;
        driver.LastLongitude = longitude;
        driver.LastLocationAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Save to location history if delivering
        if (driver.Status == nameof(DriverStatus.Delivering))
        {
            var activeAssignment = await _db.DeliveryAssignments
                .FirstOrDefaultAsync(a => a.DeliveryDriverId == driver.Id
                    && a.Status == nameof(DeliveryAssignmentStatus.Accepted)
                    || a.Status == nameof(DeliveryAssignmentStatus.PickedUp)
                    || a.Status == nameof(DeliveryAssignmentStatus.InTransit));

            await _db.DriverLocationHistories.AddAsync(new DriverLocationHistory
            {
                DeliveryDriverId = driver.Id,
                DeliveryAssignmentId = activeAssignment?.Id,
                Latitude = latitude,
                Longitude = longitude,
                RecordedAt = DateTime.UtcNow,
                CreatedBy = "system"
            });
            await _db.SaveChangesAsync();

            // Broadcast location to customer watching this delivery
            if (activeAssignment is not null)
            {
                await Clients.Group($"delivery-{activeAssignment.Id}")
                    .SendAsync("DriverLocationUpdate", new { latitude, longitude, timestamp = DateTime.UtcNow });
            }
        }
    }

    public async Task WatchDelivery(int assignmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"delivery-{assignmentId}");
    }

    public async Task UnwatchDelivery(int assignmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"delivery-{assignmentId}");
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
