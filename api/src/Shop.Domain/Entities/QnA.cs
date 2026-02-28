using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_QnAs")]
public class QnA : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public bool IsAnswered { get; set; }

    public bool IsSecret { get; set; }

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public QnAReply? Reply { get; set; }
}

[Table("SP_QnAReplies")]
public class QnAReply : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int QnAId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("QnAId")]
    public QnA QnA { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
