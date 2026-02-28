using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_OrderItems")]
public class OrderItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int? VariantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalPrice { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("VariantId")]
    public ProductVariant? Variant { get; set; }
}
