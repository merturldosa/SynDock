using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ChatMessages")]
public class ChatMessage : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int ChatRoomId { get; set; }
    public int SenderId { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [MaxLength(20)] public string MessageType { get; set; } = "Text"; // Text, Gift, Image, ProductLink
    public int? GiftId { get; set; } // If this message is a gift notification
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("ChatRoomId")] public virtual ChatRoom ChatRoom { get; set; } = null!;
    [ForeignKey("SenderId")] public virtual User Sender { get; set; } = null!;
}
