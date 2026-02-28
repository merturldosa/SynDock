using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_CartItems")]
public class CartItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int CartId { get; set; }

    public int ProductId { get; set; }

    public int? VariantId { get; set; }

    public int Quantity { get; set; }

    // Navigation
    [ForeignKey("CartId")]
    public Cart Cart { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("VariantId")]
    public ProductVariant? Variant { get; set; }
}
