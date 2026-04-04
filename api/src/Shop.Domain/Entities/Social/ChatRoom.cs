using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ChatRooms")]
public class ChatRoom : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public int? ProductId { get; set; } // Context: which product brought them together
    public int? ReviewId { get; set; } // Context: which review started the conversation
    public DateTime? LastMessageAt { get; set; }
    [MaxLength(500)] public string? LastMessagePreview { get; set; }
    public int UnreadCount1 { get; set; } // Unread for User1
    public int UnreadCount2 { get; set; } // Unread for User2
    public bool IsActive { get; set; } = true;
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("User1Id")] public virtual User User1 { get; set; } = null!;
    [ForeignKey("User2Id")] public virtual User User2 { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product? Product { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
