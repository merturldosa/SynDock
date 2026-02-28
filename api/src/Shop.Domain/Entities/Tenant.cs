using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Tenants")]
public class Tenant : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomDomain { get; set; }

    [MaxLength(50)]
    public string? Subdomain { get; set; }

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "jsonb")]
    public string? ConfigJson { get; set; }
}
