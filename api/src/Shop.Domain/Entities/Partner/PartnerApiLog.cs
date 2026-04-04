using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PartnerApiLogs")]
public class PartnerApiLog : BaseEntity
{
    public int? ApiPartnerId { get; set; }
    [MaxLength(50)] public string? PartnerCode { get; set; }
    [Required, MaxLength(10)] public string HttpMethod { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string RequestPath { get; set; } = string.Empty;
    [MaxLength(50)] public string? ClientIp { get; set; }
    public int ResponseStatus { get; set; }
    public long ResponseTimeMs { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    [MaxLength(64)] public string? RequestSignature { get; set; } // HMAC signature sent
    public bool SignatureValid { get; set; } = true;
    [MaxLength(2000)] public string? RequestBodyPreview { get; set; } // First 2000 chars (no sensitive data)
}
