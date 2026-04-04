using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Bookings")]
public class Booking : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string BookingNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public int? UserId { get; set; }
    [Required, MaxLength(100)] public string GuestName { get; set; } = string.Empty;
    [MaxLength(20)] public string? GuestPhone { get; set; }
    [MaxLength(200)] public string? GuestEmail { get; set; }
    public int GuestCount { get; set; } = 1;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Reserved"; // Reserved, CheckedIn, CheckedOut, Cancelled, NoShow
    [Column(TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal PaidAmount { get; set; }
    [MaxLength(20)] public string PaymentStatus { get; set; } = "Pending"; // Pending, Partial, Paid, Refunded
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    [MaxLength(500)] public string? SpecialRequests { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("RoomId")] public virtual Room Room { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User? User { get; set; }
}
