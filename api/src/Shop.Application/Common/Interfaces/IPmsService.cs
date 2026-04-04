using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IPmsService
{
    // Rooms
    Task<List<Room>> GetRoomsAsync(int tenantId, string? status = null, string? roomType = null, CancellationToken ct = default);
    Task<Room?> GetRoomAsync(int tenantId, int roomId, CancellationToken ct = default);
    Task<Room> CreateRoomAsync(int tenantId, string roomNumber, string roomType, string? roomName, int floor, int maxGuests, int bedCount, string bedType, decimal basePrice, decimal? weekendPrice, string? description, string? amenities, string createdBy, CancellationToken ct = default);
    Task UpdateRoomStatusAsync(int tenantId, int roomId, string status, string updatedBy, CancellationToken ct = default);

    // Bookings
    Task<Booking> CreateBookingAsync(int tenantId, int roomId, string guestName, string? guestPhone, string? guestEmail, int guestCount, DateTime checkIn, DateTime checkOut, string? specialRequests, int? userId, string createdBy, CancellationToken ct = default);
    Task<Booking?> GetBookingAsync(int tenantId, int bookingId, CancellationToken ct = default);
    Task<(List<Booking> Items, int TotalCount)> GetBookingsAsync(int tenantId, string? status = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task CheckInAsync(int tenantId, int bookingId, string updatedBy, CancellationToken ct = default);
    Task CheckOutAsync(int tenantId, int bookingId, string updatedBy, CancellationToken ct = default);
    Task CancelBookingAsync(int tenantId, int bookingId, string? reason, string updatedBy, CancellationToken ct = default);

    // Cleaning
    Task<CleaningTask> CreateCleaningTaskAsync(int tenantId, int roomId, int? bookingId, string taskType, string priority, int? assignedToUserId, string? assignedToName, string createdBy, CancellationToken ct = default);
    Task<List<CleaningTask>> GetCleaningTasksAsync(int tenantId, string? status = null, int? assignedToUserId = null, CancellationToken ct = default);
    Task StartCleaningAsync(int tenantId, int taskId, string updatedBy, CancellationToken ct = default);
    Task CompleteCleaningAsync(int tenantId, int taskId, string? checklistJson, string updatedBy, CancellationToken ct = default);
    Task InspectCleaningAsync(int tenantId, int taskId, bool passed, string? issueDescription, int inspectorUserId, string updatedBy, CancellationToken ct = default);

    // Amenities
    Task LogAmenityAsync(int tenantId, int roomId, int? cleaningTaskId, string itemName, int quantity, string actionType, string? notes, string createdBy, CancellationToken ct = default);
    Task<List<RoomAmenityLog>> GetAmenityLogsAsync(int tenantId, int? roomId = null, CancellationToken ct = default);

    // Dashboard
    Task<object> GetPmsDashboardAsync(int tenantId, CancellationToken ct = default);
    Task<List<Room>> GetRoomsForCleanerAsync(int tenantId, int cleanerUserId, CancellationToken ct = default);
}
