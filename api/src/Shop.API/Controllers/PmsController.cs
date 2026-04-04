using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class PmsController : ControllerBase
{
    private readonly IPmsService _pms;
    private readonly ICurrentUserService _currentUser;

    public PmsController(IPmsService pms, ICurrentUserService currentUser)
    {
        _pms = pms;
        _currentUser = currentUser;
    }

    private string Actor => _currentUser.Username ?? "system";

    // === Rooms ===

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms([FromQuery] string? status, [FromQuery] string? roomType, CancellationToken ct)
        => Ok(await _pms.GetRoomsAsync(0, status, roomType, ct));

    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRoom(int roomId, CancellationToken ct)
    {
        var room = await _pms.GetRoomAsync(0, roomId, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req, CancellationToken ct)
        => Ok(await _pms.CreateRoomAsync(0, req.RoomNumber, req.RoomType, req.RoomName, req.Floor, req.MaxGuests, req.BedCount, req.BedType, req.BasePrice, req.WeekendPrice, req.Description, req.Amenities, Actor, ct));

    [HttpPut("rooms/{roomId}/status")]
    public async Task<IActionResult> UpdateRoomStatus(int roomId, [FromBody] UpdateRoomStatusRequest req, CancellationToken ct)
    {
        await _pms.UpdateRoomStatusAsync(0, roomId, req.Status, Actor, ct);
        return Ok(new { message = "Room status updated" });
    }

    // === Bookings ===

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, totalCount) = await _pms.GetBookingsAsync(0, status, from, to, page, pageSize, ct);
        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("bookings/{bookingId}")]
    public async Task<IActionResult> GetBooking(int bookingId, CancellationToken ct)
    {
        var booking = await _pms.GetBookingAsync(0, bookingId, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        return Ok(await _pms.CreateBookingAsync(0, req.RoomId, req.GuestName, req.GuestPhone, req.GuestEmail, req.GuestCount, req.CheckInDate, req.CheckOutDate, req.SpecialRequests, req.UserId ?? _currentUser.UserId, Actor, ct));
    }

    [HttpPost("bookings/{bookingId}/checkin")]
    public async Task<IActionResult> CheckIn(int bookingId, CancellationToken ct)
    {
        await _pms.CheckInAsync(0, bookingId, Actor, ct);
        return Ok(new { message = "Guest checked in" });
    }

    [HttpPost("bookings/{bookingId}/checkout")]
    public async Task<IActionResult> CheckOut(int bookingId, CancellationToken ct)
    {
        await _pms.CheckOutAsync(0, bookingId, Actor, ct);
        return Ok(new { message = "Guest checked out, cleaning task created" });
    }

    [HttpPost("bookings/{bookingId}/cancel")]
    public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest? req, CancellationToken ct)
    {
        await _pms.CancelBookingAsync(0, bookingId, req?.Reason, Actor, ct);
        return Ok(new { message = "Booking cancelled" });
    }

    // === Cleaning ===

    [HttpGet("cleaning")]
    public async Task<IActionResult> GetCleaningTasks([FromQuery] string? status, [FromQuery] int? assignedToUserId, CancellationToken ct)
        => Ok(await _pms.GetCleaningTasksAsync(0, status, assignedToUserId, ct));

    [HttpPost("cleaning")]
    public async Task<IActionResult> CreateCleaningTask([FromBody] CreateCleaningTaskRequest req, CancellationToken ct)
        => Ok(await _pms.CreateCleaningTaskAsync(0, req.RoomId, req.BookingId, req.TaskType, req.Priority, req.AssignedToUserId, req.AssignedToName, Actor, ct));

    [HttpPost("cleaning/{taskId}/start")]
    public async Task<IActionResult> StartCleaning(int taskId, CancellationToken ct)
    {
        await _pms.StartCleaningAsync(0, taskId, Actor, ct);
        return Ok(new { message = "Cleaning started" });
    }

    [HttpPost("cleaning/{taskId}/complete")]
    public async Task<IActionResult> CompleteCleaning(int taskId, [FromBody] CompleteCleaningRequest? req, CancellationToken ct)
    {
        await _pms.CompleteCleaningAsync(0, taskId, req?.ChecklistJson, Actor, ct);
        return Ok(new { message = "Cleaning completed, room is now available" });
    }

    [HttpPost("cleaning/{taskId}/inspect")]
    public async Task<IActionResult> InspectCleaning(int taskId, [FromBody] InspectCleaningRequest req, CancellationToken ct)
    {
        await _pms.InspectCleaningAsync(0, taskId, req.Passed, req.IssueDescription, _currentUser.UserId ?? 0, Actor, ct);
        return Ok(new { message = req.Passed ? "Inspection passed" : "Inspection failed, issue reported" });
    }

    // === Amenities ===

    [HttpGet("amenities")]
    public async Task<IActionResult> GetAmenityLogs([FromQuery] int? roomId, CancellationToken ct)
        => Ok(await _pms.GetAmenityLogsAsync(0, roomId, ct));

    [HttpPost("amenities")]
    public async Task<IActionResult> LogAmenity([FromBody] LogAmenityRequest req, CancellationToken ct)
    {
        await _pms.LogAmenityAsync(0, req.RoomId, req.CleaningTaskId, req.ItemName, req.Quantity, req.ActionType, req.Notes, Actor, ct);
        return Ok(new { message = "Amenity logged" });
    }

    // === Dashboard ===

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
        => Ok(await _pms.GetPmsDashboardAsync(0, ct));

    [HttpGet("cleaner/rooms")]
    [Authorize] // Any authenticated user (cleaner role)
    public async Task<IActionResult> GetCleanerRooms(CancellationToken ct)
    {
        return Ok(await _pms.GetRoomsForCleanerAsync(0, _currentUser.UserId ?? 0, ct));
    }
}

// === Request DTOs ===

public record CreateRoomRequest(
    string RoomNumber,
    string RoomType = "Standard",
    string? RoomName = null,
    int Floor = 1,
    int MaxGuests = 2,
    int BedCount = 1,
    string BedType = "Double",
    decimal BasePrice = 0,
    decimal? WeekendPrice = null,
    string? Description = null,
    string? Amenities = null
);

public record UpdateRoomStatusRequest(string Status);

public record CreateBookingRequest(
    int RoomId,
    string GuestName,
    string? GuestPhone = null,
    string? GuestEmail = null,
    int GuestCount = 1,
    DateTime CheckInDate = default,
    DateTime CheckOutDate = default,
    string? SpecialRequests = null,
    int? UserId = null
);

public record CancelBookingRequest(string? Reason = null);

public record CreateCleaningTaskRequest(
    int RoomId,
    int? BookingId = null,
    string TaskType = "Checkout",
    string Priority = "Normal",
    int? AssignedToUserId = null,
    string? AssignedToName = null
);

public record CompleteCleaningRequest(string? ChecklistJson = null);

public record InspectCleaningRequest(bool Passed, string? IssueDescription = null);

public record LogAmenityRequest(
    int RoomId,
    int? CleaningTaskId = null,
    string ItemName = "",
    int Quantity = 1,
    string ActionType = "Restocked",
    string? Notes = null
);
