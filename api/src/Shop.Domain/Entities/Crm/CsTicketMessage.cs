using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CsTicketMessages")]
public class CsTicketMessage : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int CsTicketId { get; set; }
    public int SenderId { get; set; }

    [Required, MaxLength(20)]
    public string SenderType { get; set; } = "Customer"; // Customer, Agent, System

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }

    public bool IsInternal { get; set; } // Internal note (not visible to customer)

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("CsTicketId")]
    public virtual CsTicket Ticket { get; set; } = null!;

    [ForeignKey("SenderId")]
    public virtual User Sender { get; set; } = null!;
}
