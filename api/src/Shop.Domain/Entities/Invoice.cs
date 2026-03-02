using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Invoices")]
public class Invoice : BaseEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Paid, Failed, Cancelled

    [Required]
    [MaxLength(7)]
    public string BillingPeriod { get; set; } = string.Empty; // yyyy-MM

    [MaxLength(20)]
    public string? PlanType { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    [MaxLength(100)]
    public string? TransactionId { get; set; }

    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
