using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Addresses")]
public class Address : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Address1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Address2 { get; set; }

    public bool IsDefault { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
