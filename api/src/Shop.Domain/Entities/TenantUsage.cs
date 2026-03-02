using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_TenantUsages")]
public class TenantUsage : BaseEntity
{
    public int TenantId { get; set; }

    public int ProductCount { get; set; }

    public int UserCount { get; set; }

    public long StorageUsedBytes { get; set; }

    public int MonthlyOrderCount { get; set; }

    /// <summary>YYYY-MM format for the current billing period</summary>
    [MaxLength(7)]
    public string CurrentPeriod { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
