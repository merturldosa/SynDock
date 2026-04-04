using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Delivery.Commands;
using Shop.Application.Delivery.Queries;
using Shop.Domain.Entities;
using Shop.Domain.Enums;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IShopDbContext _db;

    public DeliveryController(IMediator mediator, IShopDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    // ── Driver Management ─────────────────────────────────────────

    [HttpPost("driver/register")]
    public async Task<IActionResult> RegisterDriver([FromBody] RegisterDriverCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(new { driverId = result.Data }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("driver/profile")]
    public async Task<IActionResult> GetDriverProfile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var driver = await _db.DeliveryDrivers
            .Include(d => d.User)
            .Include(d => d.ZoneDrivers).ThenInclude(zd => zd.Zone)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver is null) return NotFound(new { error = "Driver profile not found." });

        return Ok(new
        {
            driver.Id,
            driver.Phone,
            driver.Status,
            driver.VehicleType,
            driver.LicensePlate,
            driver.LicenseNumber,
            driver.IsApproved,
            driver.IsActive,
            driver.AverageRating,
            driver.TotalDeliveries,
            driver.LastLatitude,
            driver.LastLongitude,
            zones = driver.ZoneDrivers.Select(zd => new { zd.Zone.Id, zd.Zone.Name })
        });
    }

    [HttpPut("driver/profile")]
    public async Task<IActionResult> UpdateDriverProfile([FromBody] UpdateDriverProfileCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("driver/status")]
    public async Task<IActionResult> UpdateDriverStatus([FromBody] UpdateDriverStatusCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("driver/location")]
    public async Task<IActionResult> UpdateDriverLocation([FromBody] LocationRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var driver = await _db.DeliveryDrivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver is null) return NotFound();

        var locationService = HttpContext.RequestServices.GetRequiredService<IDriverLocationService>();
        await locationService.UpdateLocation(driver.Id, request.Latitude, request.Longitude);

        driver.LastLatitude = request.Latitude;
        driver.LastLongitude = request.Longitude;
        driver.LastLocationAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpGet("driver/deliveries")]
    public async Task<IActionResult> GetDriverDeliveries([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var driver = await _db.DeliveryDrivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver is null) return NotFound();

        var query = _db.DeliveryAssignments
            .Include(a => a.Order)
            .Include(a => a.DeliveryOption)
            .Where(a => a.DeliveryDriverId == driver.Id)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.OrderId,
                OrderNumber = a.Order.OrderNumber,
                a.Status,
                DeliveryType = a.DeliveryOption != null ? a.DeliveryOption.DeliveryType : null,
                a.AcceptedAt,
                a.PickedUpAt,
                a.DeliveredAt,
                a.EstimatedDeliveryAt,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("driver/{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GetDriverById(int id)
    {
        var driver = await _db.DeliveryDrivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver is null) return NotFound();

        return Ok(new
        {
            driver.Id,
            driver.UserId,
            Username = driver.User.Username,
            driver.Phone,
            driver.Status,
            driver.VehicleType,
            driver.LicensePlate,
            driver.IsApproved,
            driver.IsActive,
            driver.AverageRating,
            driver.TotalDeliveries
        });
    }

    // ── Zone Management ───────────────────────────────────────────

    [HttpGet("zones")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> GetZones()
    {
        var zones = await _db.DeliveryZones
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .Select(z => new
            {
                z.Id, z.Name, z.Description,
                z.CenterLatitude, z.CenterLongitude, z.RadiusKm,
                DriverCount = z.ZoneDrivers.Count
            })
            .ToListAsync();

        return Ok(zones);
    }

    [HttpPost("zones")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> CreateZone([FromBody] CreateZoneRequest request)
    {
        var zone = new DeliveryZone
        {
            Name = request.Name,
            Description = request.Description,
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            RadiusKm = request.RadiusKm,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        await _db.DeliveryZones.AddAsync(zone);
        await _db.SaveChangesAsync();

        return Ok(new { zone.Id });
    }

    [HttpPut("zones/{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] CreateZoneRequest request)
    {
        var zone = await _db.DeliveryZones.FindAsync(id);
        if (zone is null) return NotFound();

        zone.Name = request.Name;
        zone.Description = request.Description;
        zone.CenterLatitude = request.CenterLatitude;
        zone.CenterLongitude = request.CenterLongitude;
        zone.RadiusKm = request.RadiusKm;
        zone.UpdatedBy = User.Identity?.Name ?? "system";
        zone.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("zones/{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var zone = await _db.DeliveryZones.FindAsync(id);
        if (zone is null) return NotFound();

        zone.IsActive = false;
        zone.UpdatedBy = User.Identity?.Name ?? "system";
        zone.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost("zones/{zoneId:int}/drivers")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin,Driver")]
    public async Task<IActionResult> AddDriverToZone(int zoneId, [FromBody] ZoneDriverRequest request)
    {
        var exists = await _db.DeliveryZoneDrivers
            .AnyAsync(zd => zd.DeliveryZoneId == zoneId && zd.DeliveryDriverId == request.DriverId);

        if (exists) return BadRequest(new { error = "Driver already assigned to this zone." });

        await _db.DeliveryZoneDrivers.AddAsync(new DeliveryZoneDriver
        {
            DeliveryZoneId = zoneId,
            DeliveryDriverId = request.DriverId,
            CreatedBy = User.Identity?.Name ?? "system"
        });
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("zones/{zoneId:int}/drivers/{driverId:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin,Driver")]
    public async Task<IActionResult> RemoveDriverFromZone(int zoneId, int driverId)
    {
        var zd = await _db.DeliveryZoneDrivers
            .FirstOrDefaultAsync(z => z.DeliveryZoneId == zoneId && z.DeliveryDriverId == driverId);

        if (zd is null) return NotFound();

        _db.DeliveryZoneDrivers.Remove(zd);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // ── Delivery Options ──────────────────────────────────────────

    [HttpGet("options")]
    public async Task<IActionResult> GetDeliveryOptions()
    {
        var result = await _mediator.Send(new GetDeliveryOptionsQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("options")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> CreateDeliveryOption([FromBody] CreateDeliveryOptionRequest request)
    {
        var option = new DeliveryOption
        {
            DeliveryType = request.DeliveryType,
            DisplayName = request.DisplayName,
            Description = request.Description,
            AdditionalFee = request.AdditionalFee,
            MaxDeliveryMinutes = request.MaxDeliveryMinutes,
            MaxDistanceKm = request.MaxDistanceKm,
            AvailableFrom = request.AvailableFrom,
            AvailableTo = request.AvailableTo,
            SortOrder = request.SortOrder,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        await _db.DeliveryOptions.AddAsync(option);
        await _db.SaveChangesAsync();

        return Ok(new { option.Id });
    }

    [HttpPut("options/{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> UpdateDeliveryOption(int id, [FromBody] CreateDeliveryOptionRequest request)
    {
        var option = await _db.DeliveryOptions.FindAsync(id);
        if (option is null) return NotFound();

        option.DeliveryType = request.DeliveryType;
        option.DisplayName = request.DisplayName;
        option.Description = request.Description;
        option.AdditionalFee = request.AdditionalFee;
        option.MaxDeliveryMinutes = request.MaxDeliveryMinutes;
        option.MaxDistanceKm = request.MaxDistanceKm;
        option.AvailableFrom = request.AvailableFrom;
        option.AvailableTo = request.AvailableTo;
        option.SortOrder = request.SortOrder;
        option.UpdatedBy = User.Identity?.Name ?? "system";
        option.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("options/{id:int}")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> DeleteDeliveryOption(int id)
    {
        var option = await _db.DeliveryOptions.FindAsync(id);
        if (option is null) return NotFound();

        option.IsActive = false;
        option.UpdatedBy = User.Identity?.Name ?? "system";
        option.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // ── Assignment & Tracking ─────────────────────────────────────

    [HttpGet("orders/{orderId:int}/assignment")]
    public async Task<IActionResult> GetOrderAssignment(int orderId)
    {
        var result = await _mediator.Send(new GetDeliveryTrackingQuery(orderId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("orders/{orderId:int}/assign")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> ManualAssignDriver(int orderId, [FromBody] ManualAssignRequest request)
    {
        var assignment = await _db.DeliveryAssignments
            .FirstOrDefaultAsync(a => a.OrderId == orderId
                && (a.Status == nameof(DeliveryAssignmentStatus.Pending)
                    || a.Status == nameof(DeliveryAssignmentStatus.Offered)));

        if (assignment is null)
        {
            assignment = new DeliveryAssignment
            {
                OrderId = orderId,
                DeliveryDriverId = request.DriverId,
                Status = nameof(DeliveryAssignmentStatus.Offered),
                OfferedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system"
            };
            await _db.DeliveryAssignments.AddAsync(assignment);
        }
        else
        {
            assignment.DeliveryDriverId = request.DriverId;
            assignment.Status = nameof(DeliveryAssignmentStatus.Offered);
            assignment.OfferedAt = DateTime.UtcNow;
            assignment.UpdatedBy = User.Identity?.Name ?? "system";
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var notifier = HttpContext.RequestServices.GetRequiredService<IDriverNotifier>();
        await notifier.NotifyDriverDirectly(request.DriverId, "NewDeliveryOffer", new
        {
            assignmentId = assignment.Id,
            orderId
        });

        return Ok(new { assignmentId = assignment.Id });
    }

    [HttpPost("assignments/{id:int}/accept")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> AcceptDelivery(int id)
    {
        var result = await _mediator.Send(new AcceptDeliveryCommand(id));
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("assignments/{id:int}/reject")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> RejectDelivery(int id, [FromBody] RejectRequest? request)
    {
        var result = await _mediator.Send(new RejectDeliveryCommand(id, request?.Reason));
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPut("assignments/{id:int}/status")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateDeliveryStatus(int id, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateDeliveryStatusCommand(id, request.Status, request.PhotoUrl, request.Note));
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("assignments/{id:int}/tracking")]
    public async Task<IActionResult> GetDeliveryTracking(int id)
    {
        var assignment = await _db.DeliveryAssignments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment is null) return NotFound();

        var result = await _mediator.Send(new GetDeliveryTrackingQuery(assignment.OrderId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    // ── Admin: Drivers List ───────────────────────────────────────

    [HttpGet("drivers")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> ListDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.DeliveryDrivers
            .Include(d => d.User)
            .OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new
            {
                d.Id, d.UserId, Username = d.User.Username,
                d.Phone, d.Status, d.VehicleType, d.LicensePlate,
                d.IsApproved, d.IsActive, d.AverageRating, d.TotalDeliveries,
                d.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("drivers/available")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> ListAvailableDrivers()
    {
        var drivers = await _db.DeliveryDrivers
            .Include(d => d.User)
            .Where(d => d.Status == nameof(DriverStatus.Online) && d.IsApproved && d.IsActive)
            .Select(d => new
            {
                d.Id, Username = d.User.Username,
                d.VehicleType, d.LastLatitude, d.LastLongitude, d.AverageRating, d.TotalDeliveries
            })
            .ToListAsync();

        return Ok(drivers);
    }

    [HttpPut("drivers/{id:int}/approve")]
    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    public async Task<IActionResult> ApproveDriver(int id)
    {
        var driver = await _db.DeliveryDrivers.FindAsync(id);
        if (driver is null) return NotFound();

        driver.IsApproved = true;
        driver.UpdatedBy = User.Identity?.Name ?? "system";
        driver.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}

// ── Request DTOs ──────────────────────────────────────────────

public record LocationRequest(double Latitude, double Longitude);
public record CreateZoneRequest(string Name, string? Description, double CenterLatitude, double CenterLongitude, double RadiusKm);
public record ZoneDriverRequest(int DriverId);
public record CreateDeliveryOptionRequest(
    string DeliveryType, string DisplayName, string? Description,
    decimal AdditionalFee, int MaxDeliveryMinutes, double MaxDistanceKm,
    string? AvailableFrom, string? AvailableTo, int SortOrder = 0);
public record ManualAssignRequest(int DriverId);
public record RejectRequest(string? Reason);
public record UpdateDeliveryStatusRequest(string Status, string? PhotoUrl, string? Note);
