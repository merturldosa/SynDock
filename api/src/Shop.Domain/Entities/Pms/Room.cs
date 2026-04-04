using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Rooms")]
public class Room : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(20)] public string RoomNumber { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string RoomType { get; set; } = "Standard"; // Standard, Deluxe, Suite, Family, Dormitory
    [MaxLength(100)] public string? RoomName { get; set; }
    public int Floor { get; set; } = 1;
    public int MaxGuests { get; set; } = 2;
    public int BedCount { get; set; } = 1;
    [MaxLength(20)] public string BedType { get; set; } = "Double"; // Single, Double, Queen, King, Twin, Bunk
    [Column(TypeName = "decimal(18,0)")] public decimal BasePrice { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal? WeekendPrice { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal? PeakPrice { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Available"; // Available, Occupied, Cleaning, Maintenance, Blocked
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(500)] public string? Amenities { get; set; } // JSON: ["WiFi","TV","Minibar","AirCon"]
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastCleanedAt { get; set; }
    [MaxLength(50)] public string? LastCleanedBy { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
}
