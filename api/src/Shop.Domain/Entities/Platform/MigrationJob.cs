using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_MigrationJobs")]
public class MigrationJob : BaseEntity
{
    public int? TenantId { get; set; }
    public int? ApplicationId { get; set; }

    [Required, MaxLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string SourceType { get; set; } = "Website"; // Website, Cafe24, MakeShop, Shopify, CSV

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Crawling, Extracting, Importing, Completed, Failed

    public int TotalProductsFound { get; set; }
    public int TotalCategoriesFound { get; set; }
    public int TotalImagesFound { get; set; }
    public int ProductsImported { get; set; }
    public int CategoriesImported { get; set; }
    public int ImagesImported { get; set; }
    public int FailedItems { get; set; }

    [Column(TypeName = "decimal(5,1)")]
    public decimal ProgressPercent { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [Column(TypeName = "jsonb")]
    public string? CrawlResultJson { get; set; } // Raw crawl data before import

    [Column(TypeName = "jsonb")]
    public string? ImportLogJson { get; set; } // Import results/errors

    [ForeignKey("TenantId")]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("ApplicationId")]
    public virtual TenantApplication? Application { get; set; }
}
