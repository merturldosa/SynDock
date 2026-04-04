using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CsTickets")]
public class CsTicket : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(50)]
    public string TicketNumber { get; set; } = string.Empty;

    public int UserId { get; set; }
    public int? OrderId { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Category { get; set; } = "General"; // General, Order, Shipping, Return, Refund, Product, Payment, Other

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Open"; // Open, InProgress, WaitingCustomer, Resolved, Closed

    [Required, MaxLength(20)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

    public int? AssignedToUserId { get; set; }

    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int? SatisfactionRating { get; set; } // 1~5

    [MaxLength(500)]
    public string? SatisfactionComment { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("AssignedToUserId")]
    public virtual User? AssignedToUser { get; set; }

    public virtual ICollection<CsTicketMessage> Messages { get; set; } = new List<CsTicketMessage>();
}
