using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_SaintProducts")]
public class SaintProduct : BaseEntity
{
    public int SaintId { get; set; }

    public int ProductId { get; set; }

    // Navigation
    [ForeignKey("SaintId")]
    public Saint Saint { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
