using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class PmsService : IPmsService
{
    private readonly IShopDbContext _db;

    public PmsService(IShopDbContext db) => _db = db;

    // === Rooms ===

    public async Task<List<Room>> GetRoomsAsync(int tenantId, string? status = null, string? roomType = null, CancellationToken ct = default)
    {
        var query = _db.Rooms.AsNoTracking().Where(r => r.IsActive).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(roomType)) query = query.Where(r => r.RoomType == roomType);
        return await query.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToListAsync(ct);
    }

    public async Task<Room?> GetRoomAsync(int tenantId, int roomId, CancellationToken ct = default)
        => await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId, ct);

    public async Task<Room> CreateRoomAsync(int tenantId, string roomNumber, string roomType, string? roomName, int floor, int maxGuests, int bedCount, string bedType, decimal basePrice, decimal? weekendPrice, string? description, string? amenities, string createdBy, CancellationToken ct = default)
    {
        var exists = await _db.Rooms.AnyAsync(r => r.RoomNumber == roomNumber, ct);
        if (exists) throw new InvalidOperationException($"Room number '{roomNumber}' already exists");

        var room = new Room
        {
            TenantId = tenantId,
            RoomNumber = roomNumber,
            RoomType = roomType,
            RoomName = roomName,
            Floor = floor,
            MaxGuests = maxGuests,
            BedCount = bedCount,
            BedType = bedType,
            BasePrice = basePrice,
            WeekendPrice = weekendPrice,
            Description = description,
            Amenities = amenities,
            Status = "Available",
            CreatedBy = createdBy
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);
        return room;
    }

    public async Task UpdateRoomStatusAsync(int tenantId, int roomId, string status, string updatedBy, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new InvalidOperationException("Room not found");
        room.Status = status;
        room.UpdatedBy = updatedBy;
        room.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // === Bookings ===

    public async Task<Booking> CreateBookingAsync(int tenantId, int roomId, string guestName, string? guestPhone, string? guestEmail, int guestCount, DateTime checkIn, DateTime checkOut, string? specialRequests, int? userId, string createdBy, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new InvalidOperationException("Room not found");

        if (room.Status != "Available")
            throw new InvalidOperationException($"Room {room.RoomNumber} is not available (current: {room.Status})");

        if (checkOut <= checkIn)
            throw new InvalidOperationException("Check-out date must be after check-in date");

        // Check for overlapping bookings
        var hasOverlap = await _db.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            b.Status != "Cancelled" && b.Status != "NoShow" && b.Status != "CheckedOut" &&
            b.CheckInDate < checkOut && b.CheckOutDate > checkIn, ct);

        if (hasOverlap)
            throw new InvalidOperationException("Room has overlapping bookings for the requested dates");

        var nights = (int)(checkOut.Date - checkIn.Date).TotalDays;
        if (nights < 1) nights = 1;

        // Calculate total: use weekend price for Fri/Sat nights, base price otherwise
        var totalAmount = 0m;
        for (var d = checkIn.Date; d < checkOut.Date; d = d.AddDays(1))
        {
            var isWeekend = d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday;
            totalAmount += isWeekend && room.WeekendPrice.HasValue ? room.WeekendPrice.Value : room.BasePrice;
        }

        var bookingNumber = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var booking = new Booking
        {
            TenantId = tenantId,
            BookingNumber = bookingNumber,
            RoomId = roomId,
            UserId = userId,
            GuestName = guestName,
            GuestPhone = guestPhone,
            GuestEmail = guestEmail,
            GuestCount = guestCount,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Nights = nights,
            Status = "Reserved",
            TotalAmount = totalAmount,
            PaidAmount = 0,
            PaymentStatus = "Pending",
            SpecialRequests = specialRequests,
            CreatedBy = createdBy
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking?> GetBookingAsync(int tenantId, int bookingId, CancellationToken ct = default)
        => await _db.Bookings.AsNoTracking().Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public async Task<(List<Booking> Items, int TotalCount)> GetBookingsAsync(int tenantId, string? status = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.Bookings.AsNoTracking().Include(b => b.Room).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
        if (from.HasValue) query = query.Where(b => b.CheckOutDate >= from.Value);
        if (to.HasValue) query = query.Where(b => b.CheckInDate <= to.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(b => b.CheckInDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task CheckInAsync(int tenantId, int bookingId, string updatedBy, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException("Booking not found");

        if (booking.Status != "Reserved")
            throw new InvalidOperationException($"Cannot check in: booking status is '{booking.Status}'");

        booking.Status = "CheckedIn";
        booking.ActualCheckIn = DateTime.UtcNow;
        booking.UpdatedBy = updatedBy;
        booking.UpdatedAt = DateTime.UtcNow;

        booking.Room.Status = "Occupied";
        booking.Room.UpdatedBy = updatedBy;
        booking.Room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task CheckOutAsync(int tenantId, int bookingId, string updatedBy, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException("Booking not found");

        if (booking.Status != "CheckedIn")
            throw new InvalidOperationException($"Cannot check out: booking status is '{booking.Status}'");

        booking.Status = "CheckedOut";
        booking.ActualCheckOut = DateTime.UtcNow;
        booking.UpdatedBy = updatedBy;
        booking.UpdatedAt = DateTime.UtcNow;

        // Set room to Cleaning
        booking.Room.Status = "Cleaning";
        booking.Room.UpdatedBy = updatedBy;
        booking.Room.UpdatedAt = DateTime.UtcNow;

        // Auto-create cleaning task
        var cleaningTask = new CleaningTask
        {
            TenantId = tenantId,
            RoomId = booking.RoomId,
            BookingId = bookingId,
            TaskType = "Checkout",
            Status = "Pending",
            Priority = "High",
            CreatedBy = updatedBy
        };
        _db.CleaningTasks.Add(cleaningTask);

        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelBookingAsync(int tenantId, int bookingId, string? reason, string updatedBy, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException("Booking not found");

        if (booking.Status == "CheckedOut" || booking.Status == "Cancelled")
            throw new InvalidOperationException($"Cannot cancel: booking status is '{booking.Status}'");

        booking.Status = "Cancelled";
        booking.Notes = string.IsNullOrEmpty(reason) ? booking.Notes : $"[Cancelled] {reason}";
        booking.UpdatedBy = updatedBy;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // === Cleaning ===

    public async Task<CleaningTask> CreateCleaningTaskAsync(int tenantId, int roomId, int? bookingId, string taskType, string priority, int? assignedToUserId, string? assignedToName, string createdBy, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, ct)
            ?? throw new InvalidOperationException("Room not found");

        var task = new CleaningTask
        {
            TenantId = tenantId,
            RoomId = roomId,
            BookingId = bookingId,
            TaskType = taskType,
            Status = "Pending",
            Priority = priority,
            AssignedToUserId = assignedToUserId,
            AssignedToName = assignedToName,
            CreatedBy = createdBy
        };
        _db.CleaningTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<List<CleaningTask>> GetCleaningTasksAsync(int tenantId, string? status = null, int? assignedToUserId = null, CancellationToken ct = default)
    {
        var query = _db.CleaningTasks.AsNoTracking().Include(t => t.Room).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        if (assignedToUserId.HasValue) query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task StartCleaningAsync(int tenantId, int taskId, string updatedBy, CancellationToken ct = default)
    {
        var task = await _db.CleaningTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException("Cleaning task not found");

        if (task.Status != "Pending")
            throw new InvalidOperationException($"Cannot start: task status is '{task.Status}'");

        task.Status = "InProgress";
        task.StartedAt = DateTime.UtcNow;
        task.UpdatedBy = updatedBy;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteCleaningAsync(int tenantId, int taskId, string? checklistJson, string updatedBy, CancellationToken ct = default)
    {
        var task = await _db.CleaningTasks.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException("Cleaning task not found");

        if (task.Status != "InProgress")
            throw new InvalidOperationException($"Cannot complete: task status is '{task.Status}'");

        task.Status = "Completed";
        task.CompletedAt = DateTime.UtcNow;
        task.ChecklistJson = checklistJson;
        task.UpdatedBy = updatedBy;
        task.UpdatedAt = DateTime.UtcNow;

        // Update room status and cleaning info
        task.Room.Status = "Available";
        task.Room.LastCleanedAt = DateTime.UtcNow;
        task.Room.LastCleanedBy = updatedBy;
        task.Room.UpdatedBy = updatedBy;
        task.Room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task InspectCleaningAsync(int tenantId, int taskId, bool passed, string? issueDescription, int inspectorUserId, string updatedBy, CancellationToken ct = default)
    {
        var task = await _db.CleaningTasks.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new InvalidOperationException("Cleaning task not found");

        if (task.Status != "Completed")
            throw new InvalidOperationException($"Cannot inspect: task status is '{task.Status}'");

        task.InspectedAt = DateTime.UtcNow;
        task.InspectedByUserId = inspectorUserId;
        task.UpdatedBy = updatedBy;
        task.UpdatedAt = DateTime.UtcNow;

        if (passed)
        {
            task.Status = "Inspected";
            task.Room.Status = "Available";
        }
        else
        {
            task.Status = "Issue";
            task.IssueDescription = issueDescription;
            task.Room.Status = "Cleaning"; // Keep in cleaning
        }

        task.Room.UpdatedBy = updatedBy;
        task.Room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // === Amenities ===

    public async Task LogAmenityAsync(int tenantId, int roomId, int? cleaningTaskId, string itemName, int quantity, string actionType, string? notes, string createdBy, CancellationToken ct = default)
    {
        var log = new RoomAmenityLog
        {
            TenantId = tenantId,
            RoomId = roomId,
            CleaningTaskId = cleaningTaskId,
            ItemName = itemName,
            Quantity = quantity,
            ActionType = actionType,
            Notes = notes,
            CreatedBy = createdBy
        };
        _db.RoomAmenityLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<RoomAmenityLog>> GetAmenityLogsAsync(int tenantId, int? roomId = null, CancellationToken ct = default)
    {
        var query = _db.RoomAmenityLogs.AsNoTracking().Include(l => l.Room).AsQueryable();
        if (roomId.HasValue) query = query.Where(l => l.RoomId == roomId.Value);
        return await query.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync(ct);
    }

    // === Dashboard ===

    public async Task<object> GetPmsDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var rooms = await _db.Rooms.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayCheckIns = await _db.Bookings.AsNoTracking()
            .CountAsync(b => b.Status == "Reserved" && b.CheckInDate.Date == today, ct);

        var todayCheckOuts = await _db.Bookings.AsNoTracking()
            .CountAsync(b => b.Status == "CheckedIn" && b.CheckOutDate.Date == today, ct);

        var pendingCleaning = await _db.CleaningTasks.AsNoTracking()
            .CountAsync(t => t.Status == "Pending" || t.Status == "InProgress", ct);

        var totalRooms = rooms.Count;
        var availableRooms = rooms.Count(r => r.Status == "Available");
        var occupiedRooms = rooms.Count(r => r.Status == "Occupied");
        var cleaningRooms = rooms.Count(r => r.Status == "Cleaning");
        var maintenanceRooms = rooms.Count(r => r.Status == "Maintenance");
        var occupancyRate = totalRooms > 0 ? Math.Round((double)occupiedRooms / totalRooms * 100, 1) : 0;

        return new
        {
            rooms = new { total = totalRooms, available = availableRooms, occupied = occupiedRooms, cleaning = cleaningRooms, maintenance = maintenanceRooms },
            todayCheckIns,
            todayCheckOuts,
            pendingCleaning,
            occupancyRate
        };
    }

    public async Task<List<Room>> GetRoomsForCleanerAsync(int tenantId, int cleanerUserId, CancellationToken ct = default)
    {
        var roomIds = await _db.CleaningTasks.AsNoTracking()
            .Where(t => t.AssignedToUserId == cleanerUserId && (t.Status == "Pending" || t.Status == "InProgress"))
            .Select(t => t.RoomId)
            .Distinct()
            .ToListAsync(ct);

        return await _db.Rooms.AsNoTracking()
            .Where(r => roomIds.Contains(r.Id))
            .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
            .ToListAsync(ct);
    }
}
